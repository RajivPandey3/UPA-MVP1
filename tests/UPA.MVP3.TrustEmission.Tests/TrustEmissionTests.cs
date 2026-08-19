using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using UPA.MVP3.TrustEmission;
using UPA.VerificationTrustAnchor;

namespace UPA.MVP3.TrustEmission.Tests
{
    public class TrustEmissionTests : IDisposable
    {
        private readonly string _tempDir;
        
        public TrustEmissionTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }

        private string GetStateFilePath() => Path.Combine(_tempDir, "processed-runs.json");

        [Fact]
        public void T01_CanonicalEncoding_UsesUtf8Lengths_AndPreventsCollision()
        {
            // Arrange
            // Create a malicious RunId that tries to bleed into the AuditHash
            string runIdMalicious = "run-1\nAUDIT:64:abcdef";
            string auditHashReal = new string('0', 64);
            
            // Act
            string encoded1 = EvidenceEncoder.Encode(runIdMalicious, auditHashReal);
            
            // Expected canonical encoding uses exact UTF-8 lengths.
            int expectedRunIdLength = Encoding.UTF8.GetByteCount(runIdMalicious);
            int expectedAuditHashLength = Encoding.UTF8.GetByteCount(auditHashReal);
            
            string expectedEncoding = $"RUNID:{expectedRunIdLength}:{runIdMalicious}\nAUDIT:{expectedAuditHashLength}:{auditHashReal}";
            
            // Assert
            Assert.Equal(expectedEncoding, encoded1);
        }

        [Fact]
        public async Task T02_StandardEmission_RegistersSequence1_Successfully()
        {
            // Arrange
            var registry = new RegistryCertificateChain();
            var emitter = new TrustEmitter(GetStateFilePath(), registry);
            var req = new TrustEmissionRequest("run-1", "bundle-1", "hash-1", "audit-data");
            
            // Act
            var cert = await emitter.EmitAsync(req);
            
            // Assert
            Assert.NotNull(cert);
            Assert.Single(registry.Entries);
            var entry = registry.Entries.Single();
            Assert.Equal(1, entry.Sequence);
            Assert.Equal("bundle-1", entry.BundleId);
            Assert.Equal("hash-1", entry.BundleFingerprint);
            Assert.Null(entry.PreviousRegistryCertificateId);
            Assert.True(File.Exists(GetStateFilePath()));
        }

        [Fact]
        public async Task T03_DuplicateRunId_ReturnsCachedEntry_WithoutReinvokingFactory()
        {
            // Arrange
            var registry = new RegistryCertificateChain();
            var emitter = new TrustEmitter(GetStateFilePath(), registry);
            var req = new TrustEmissionRequest("run-1", "bundle-1", "hash-1", "audit-data");
            
            // Emit first time
            var cert1 = await emitter.EmitAsync(req);
            
            // Clear the MVP-2 registry manually to prove it doesn't invoke factory again if MVP-2 is empty?
            // Actually T06 is for MVP-2 restart. T03 is just returning the cached entry while MVP-2 is alive.
            var entriesBefore = registry.Entries.Count;

            // Act
            var cert2 = await emitter.EmitAsync(req);
            
            // Assert
            Assert.Equal(cert1.EntryId, cert2.EntryId);
            Assert.Equal(entriesBefore, registry.Entries.Count); // No new entry added
        }

        [Fact]
        public async Task T04_SameRunId_WithConflictingPayload_ThrowsIdempotencyConflict()
        {
            // Arrange
            var registry = new RegistryCertificateChain();
            var emitter = new TrustEmitter(GetStateFilePath(), registry);
            var req1 = new TrustEmissionRequest("run-1", "bundle-1", "hash-1", "audit-data");
            var req2Conflicting = new TrustEmissionRequest("run-1", "bundle-1", "hash-DIFFERENT", "audit-data");
            
            await emitter.EmitAsync(req1);
            
            // Act & Assert
            await Assert.ThrowsAsync<IdempotencyConflictException>(() => emitter.EmitAsync(req2Conflicting));
        }

        [Fact]
        public async Task T05_BundleCollision_DifferentRunId_ForSameBundle_ThrowsBundleCollisionException()
        {
            // Arrange
            var registry = new RegistryCertificateChain();
            var emitter = new TrustEmitter(GetStateFilePath(), registry);
            var req1 = new TrustEmissionRequest("run-1", "bundle-1", "hash-1", "audit-data");
            var req2 = new TrustEmissionRequest("run-DIFFERENT", "bundle-1", "hash-1", "audit-data-different");
            
            await emitter.EmitAsync(req1);
            
            // Act & Assert
            await Assert.ThrowsAsync<BundleCollisionException>(() => emitter.EmitAsync(req2));
        }

        [Fact]
        public async Task T06_Mvp2Restart_TriggersExactRehydration()
        {
            // Arrange
            var registry1 = new RegistryCertificateChain();
            var emitter1 = new TrustEmitter(GetStateFilePath(), registry1);
            var req = new TrustEmissionRequest("run-1", "bundle-1", "hash-1", "audit-data");
            
            var originalCert = await emitter1.EmitAsync(req);
            
            // Simulate MVP-2 Restart
            var registry2 = new RegistryCertificateChain(); // completely empty
            var emitter2 = new TrustEmitter(GetStateFilePath(), registry2);
            
            // Act
            var rehydratedCert = await emitter2.EmitAsync(req);
            
            // Assert
            Assert.Equal(originalCert.EntryId, rehydratedCert.EntryId);
            Assert.Single(registry2.Entries);
            
            var verifyResult = registry2.Verify();
            Assert.True(verifyResult.Valid);
        }

        [Fact]
        public async Task T07_RegistrationSucceeds_StateSaveFails_NextRetryHealsDiskState()
        {
            // Arrange
            var registry = new RegistryCertificateChain();
            var req = new TrustEmissionRequest("run-1", "bundle-1", "hash-1", "audit-data");
            
            // Manually inject a valid CertificateChainEntry into MVP-2, simulating a crash BEFORE disk save
            var stateFile = GetStateFilePath();
            
            // To simulate it perfectly, we need to create an entry with the EXACT RegistryFingerprint
            // But we don't have the implementation yet, so we'll just test that EmitAsync handles finding it.
            // Wait, we can't fully arrange this without knowing the exact hash algorithm if it's black-boxed.
            // But for the test, we'll run an emitter, then DELETE the state file!
            var emitterSetup = new TrustEmitter(stateFile, registry);
            var cert1 = await emitterSetup.EmitAsync(req);
            
            // Delete state file (simulate save failure / disk loss, but MVP-2 kept it in memory)
            File.Delete(stateFile);
            
            // Act
            var emitterRetry = new TrustEmitter(stateFile, registry);
            var cert2 = await emitterRetry.EmitAsync(req);
            
            // Assert
            Assert.Equal(cert1.EntryId, cert2.EntryId);
            Assert.True(File.Exists(stateFile)); // Disk state healed
        }

        [Fact]
        public async Task T08_ConcurrentSameBundleRequests_AreSerialized_AndHandledCorrectly()
        {
            // Arrange
            var registry = new RegistryCertificateChain();
            var emitter = new TrustEmitter(GetStateFilePath(), registry);
            var req1 = new TrustEmissionRequest("run-1", "bundle-1", "hash-1", "audit-data");
            var req2 = new TrustEmissionRequest("run-2", "bundle-1", "hash-1", "audit-data-diff");

            // Act
            var task1 = emitter.EmitAsync(req1);
            var task2 = emitter.EmitAsync(req2);
            
            // Wait for both to complete or throw
            Exception ex = null;
            try { await Task.WhenAll(task1, task2); } catch (Exception e) { ex = e; }
            
            // Assert
            Assert.NotNull(ex);
            Assert.IsType<BundleCollisionException>(ex);
            Assert.Single(registry.Entries); // Only one made it
        }

        [Fact]
        public async Task T09_ConcurrentDifferentBundleRequests_ProcessInParallel()
        {
            // Arrange
            var registry = new RegistryCertificateChain();
            var emitter = new TrustEmitter(GetStateFilePath(), registry);
            var req1 = new TrustEmissionRequest("run-1", "bundle-1", "hash-1", "audit-data");
            var req2 = new TrustEmissionRequest("run-2", "bundle-2", "hash-2", "audit-data-diff");

            // Act
            var task1 = emitter.EmitAsync(req1);
            var task2 = emitter.EmitAsync(req2);
            await Task.WhenAll(task1, task2);
            
            // Assert
            Assert.Equal(2, registry.Entries.Count);
        }

        [Fact]
        public async Task T10_CorruptedDurableState_FailsGracefully()
        {
            // Arrange
            var registry = new RegistryCertificateChain();
            File.WriteAllText(GetStateFilePath(), "{ corrupted json /// ");
            var emitter = new TrustEmitter(GetStateFilePath(), registry);
            var req = new TrustEmissionRequest("run-1", "bundle-1", "hash-1", "audit-data");
            
            // Act & Assert
            await Assert.ThrowsAsync<System.Text.Json.JsonException>(() => emitter.EmitAsync(req));
        }
    }
}
