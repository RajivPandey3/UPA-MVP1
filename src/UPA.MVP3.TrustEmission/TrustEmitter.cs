using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using UPA.VerificationTrustAnchor;

namespace UPA.MVP3.TrustEmission
{
    // The durable record structure for Model A
    public class ProcessedRunRecord
    {
        public required string RunId { get; set; }
        public required string ArtifactHash { get; set; }
        public required string RegistryFingerprint { get; set; }
        public required CertificateChainEntry Entry { get; set; }
    }

    public class DurableState
    {
        public Dictionary<string, ProcessedRunRecord> ProcessedRuns { get; set; } = new();
        public Dictionary<string, string> ProcessedBundles { get; set; } = new();
    }

    public class TrustEmitter
    {
        private readonly string _stateFilePath;
        private readonly RegistryCertificateChain _mvp2Registry;
        private readonly object _lock = new object();

        public TrustEmitter(string stateFilePath, RegistryCertificateChain mvp2Registry)
        {
            _stateFilePath = stateFilePath ?? throw new ArgumentNullException(nameof(stateFilePath));
            _mvp2Registry = mvp2Registry ?? throw new ArgumentNullException(nameof(mvp2Registry));
        }

        private DurableState LoadState()
        {
            if (!File.Exists(_stateFilePath)) return new DurableState();
            string json = File.ReadAllText(_stateFilePath);
            return JsonSerializer.Deserialize<DurableState>(json) ?? new DurableState();
        }

        private void SaveState(DurableState state)
        {
            string json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
            string tempPath = _stateFilePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(tempPath, json);
                File.Move(tempPath, _stateFilePath, true);
            }
            catch
            {
                if (File.Exists(tempPath))
                {
                    try { File.Delete(tempPath); } catch { }
                }
                throw;
            }
        }

        public Task<CertificateChainEntry> EmitAsync(TrustEmissionRequest request)
        {
            try
            {
            if (request == null) throw new ArgumentNullException(nameof(request));

            // 1. Evidence Encoding
            string auditHash = CanonicalHash.Sha256(request.FinalizedAuditSnapshot);
            string encodedEvidence = EvidenceEncoder.Encode(request.RunId, auditHash);
            string registryFingerprint = CanonicalHash.Sha256(encodedEvidence);

            // 2. Deterministic IDs for reconstructing expected fingerprint
            string rootId = CanonicalHash.Sha256($"ROOT:{request.ArtifactBundleId}:{registryFingerprint}");
            string certId = CanonicalHash.Sha256($"CERT:{request.ArtifactBundleId}:{registryFingerprint}");
            string genesisHash = CanonicalHash.Sha256("GENESIS_BOOTSTRAP");

            var rootInput = new ChainRootInput(
                request.ArtifactBundleId,
                request.ArtifactHash,
                1,
                genesisHash, 
                genesisHash,
                genesisHash,
                genesisHash,
                new[] { genesisHash },
                genesisHash,
                true
            );

            string expectedRootFingerprint = ChainRootFactory.Fingerprint(rootInput);

            var certInput = new RegistryCertificateInput(
                request.ArtifactBundleId,
                request.ArtifactHash,
                1,
                rootId,
                expectedRootFingerprint,
                rootId,
                expectedRootFingerprint,
                new[] { rootId },
                registryFingerprint
            );

            string expectedRegistryCertFingerprint = RegistryCertificateFactory.Fingerprint(certInput);

            DurableState state;
            lock (_lock) 
            {
                state = LoadState();

                // Idempotency Hit (Duplicate RunId)
                if (state.ProcessedRuns.TryGetValue(request.RunId, out var record))
                {
                    if (record.ArtifactHash != request.ArtifactHash || record.RegistryFingerprint != registryFingerprint)
                    {
                        throw new IdempotencyConflictException("Conflicting payload for existing RunId");
                    }

                    if (!_mvp2Registry.Entries.Any(e => e.RegistryCertificateId == record.Entry.RegistryCertificateId))
                    {
                        _mvp2Registry.Register(record.Entry);
                    }

                    return Task.FromResult(record.Entry);
                }

                // T07 Healing: Check if MVP-2 has it (from previous crash)
                var existingMvp2Entry = _mvp2Registry.Entries.FirstOrDefault(e => e.BundleId == request.ArtifactBundleId);
                if (existingMvp2Entry != null)
                {
                    if (existingMvp2Entry.RegistryCertificateFingerprint == expectedRegistryCertFingerprint)
                    {
                        // Ownership verified! Heal state.
                        state.ProcessedRuns[request.RunId] = new ProcessedRunRecord
                        {
                            RunId = request.RunId,
                            ArtifactHash = request.ArtifactHash,
                            RegistryFingerprint = registryFingerprint,
                            Entry = existingMvp2Entry
                        };
                        state.ProcessedBundles[request.ArtifactBundleId] = request.RunId;
                        SaveState(state);
                        return Task.FromResult(existingMvp2Entry);
                    }
                    else
                    {
                        // Someone else owns this bundle in MVP-2
                        throw new BundleCollisionException($"Bundle {request.ArtifactBundleId} already in MVP-2 registry by another run.");
                    }
                }

                // T05: Bundle Collision Check on Durable State
                if (state.ProcessedBundles.TryGetValue(request.ArtifactBundleId, out var owningRunId))
                {
                    throw new BundleCollisionException($"Bundle {request.ArtifactBundleId} is already owned by Run {owningRunId}");
                }

                // 2. Exact MVP-2 factory sequence (execute since not found)
                var rootCert = ChainRootFactory.Create(rootId, rootInput, DateTimeOffset.UtcNow);
                var registryCert = RegistryCertificateFactory.Create(certId, certInput, DateTimeOffset.UtcNow);

                var entry = new CertificateChainEntry(
                    CanonicalHash.Sha256($"ENTRY:{request.ArtifactBundleId}:{registryFingerprint}"),
                    request.ArtifactBundleId,
                    request.ArtifactHash,
                    1,
                    registryCert.CertificateId,
                    registryCert.CertificateHash,
                    registryCert.RegistryCertificateFingerprint,
                    null,
                    null,
                    registryCert.CertifiedUtc
                );

                // 3. Register
                _mvp2Registry.Register(entry);

                // 4. Durable State
                state.ProcessedRuns[request.RunId] = new ProcessedRunRecord
                {
                    RunId = request.RunId,
                    ArtifactHash = request.ArtifactHash,
                    RegistryFingerprint = registryFingerprint,
                    Entry = entry
                };
                state.ProcessedBundles[request.ArtifactBundleId] = request.RunId;
                SaveState(state);

                return Task.FromResult(entry);
            }
        }
        catch (Exception ex)
        {
            return Task.FromException<CertificateChainEntry>(ex);
        }
    }
}

    public class IdempotencyConflictException : Exception
    {
        public IdempotencyConflictException(string message) : base(message) { }
    }

    public class BundleCollisionException : Exception
    {
        public BundleCollisionException(string message) : base(message) { }
    }
}
