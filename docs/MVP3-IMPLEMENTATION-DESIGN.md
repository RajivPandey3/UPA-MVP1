# MVP-3 Implementation Design (v2)

This document serves as the implementation blueprint for the MVP-3 Trust Emission subsystem. It translates the resolved "Artifact Trust Chain" architecture into exact technical constraints.

## 1. Input Model: `TrustEmissionRequest`
The primary input to MVP-3 represents the final output of an MVP-1 builder pipeline.

```csharp
public sealed record TrustEmissionRequest(
    string RunId,                  // Unique execution identity
    string ArtifactBundleId,       // e.g., "release-v1.0.0"
    string ArtifactHash,           // Canonical hash of the generated artifact
    string FinalizedAuditSnapshot  // The immutable MVP-1 AuditTrail text
);
```

## 2. Canonical Evidence Encoding
To safely map execution evidence (`RunId` + Audit Hash) into MVP-2's `RegistryFingerprint` without ambiguity, a strict canonical byte structure must be defined.

- `AuditHash` = `CanonicalHash.Sha256(FinalizedAuditSnapshot)`
- `RunIdUtf8Bytes` = UTF-8 encoding of `RunId`
- `AuditHashUtf8Bytes` = UTF-8 encoding of `AuditHash`
- `EncodedEvidence` = `$"RUNID:{RunIdUtf8Bytes.Length}:{RunId}\nAUDIT:{AuditHashUtf8Bytes.Length}:{AuditHash}"`
- `RegistryFingerprint` = `CanonicalHash.Sha256(UTF8_Encode(EncodedEvidence))`

*Rationale: Length-prefixing using exact UTF-8 byte lengths eliminates encoding ambiguity and prevents maliciously crafted boundaries from spoofing representations.*

## 3. Sequence-1 Emission Scope & Constraint
- **Emission Rule:** MVP-3 acts exclusively as the initial "Builder Attestation". Therefore, MVP-3 ALWAYS generates and registers `Sequence = 1`.
- **Constraint:** One immutable `BundleId` may have at most ONE MVP-3 Builder Attestation. Any attempt by a different `RunId` to emit an attestation for an already-claimed `BundleId` must be rejected as a collision.

## 4. Durable Emission Record (Model A: Durable Certificate Identity)
To guarantee strict `RunId` idempotency, exact certificate identity across process restarts, and prevent `BundleId` collisions, MVP-3 must persist two lookups in its durable state (e.g., `processed-runs.json`):

1. **ProcessedRuns:** Maps `RunId` to the full canonical `CertificateChainEntry`.
2. **ProcessedBundles:** Maps `BundleId` to the owning `RunId`.

```json
{
  "ProcessedRuns": {
    "run-1234": {
      "RunId": "run-1234",
      "ArtifactHash": "hash-x",
      "RegistryFingerprint": "evidence-fp",
      "Entry": { /* Full JSON serialized CertificateChainEntry */ }
    }
  },
  "ProcessedBundles": {
    "release-v1.0.0": "run-1234"
  }
}
```

## 5. Restart & Idempotency Protocol (State Transitions)
MVP-3 must safely handle all combinations of process restarts and failures using its durable state:

1. **Duplicate `RunId` (Idempotency Hit):**
   - **Condition:** `RunId` exists in `ProcessedRuns`.
   - **Validation:** Check if incoming `ArtifactHash` and computed `RegistryFingerprint` match the stored values. 
     - *Mismatch:* Throws `IdempotencyConflictException` (Data corruption / changed input for same RunId).
   - **Action:** MVP-3 checks MVP-2's registry for this `BundleId`. 
     - If absent (MVP-2 restarted), MVP-3 **rehydrates** MVP-2 by calling `Register()` with the exact deserialized `Entry` from disk (proven viable by `Test 2`).
   - **Result:** Exact identical certificate identity is returned.

2. **Bundle Collision (Hard Rejection):**
   - **Condition:** `RunId` absent in `ProcessedRuns`, but `BundleId` exists in `ProcessedBundles` OR in MVP-2 registry.
   - **Action:** MVP-3 immediately throws `BundleCollisionException`. (A bundle can only have one builder attestation).

3. **Crash AFTER MVP-2 Registration, BEFORE State Save:**
   - **Condition:** `RunId` absent, `BundleId` absent in `ProcessedBundles`, but `BundleId` exists in MVP-2 registry.
   - **Action:** MVP-3 retrieves the orphaned entry from MVP-2: `MVP2.Chain.Entries.Single(e => e.BundleId == req.BundleId)`.
   - **Validation:** MVP-3 must verify if this entry belongs to the current retry. It reconstructs the expected `RegistryCertificateFingerprint` by calling `RegistryCertificateFactory.Fingerprint()` using the incoming request's `RegistryFingerprint` and the entry's existing root properties.
     - *Match:* MVP-3 heals its disk state (`ProcessedRuns` + `ProcessedBundles`) with this entry, and returns success.
     - *Mismatch:* Another run claimed this `BundleId` first. Throws `BundleCollisionException`.

4. **Fresh Emission:**
   - **Condition:** `RunId` absent, `BundleId` absent in both disk and MVP-2.
   - **Action:** Clean execution of the MVP-2 factory algorithm.

## 6. MVP-2 Factory Invocation Algorithm
When executing a fresh emission (Condition 3 or 4):

1. Compute UTF-8 canonical `AuditHash` and `RegistryFingerprint`.
2. Generate `RootId` and `CertId` (`Guid.NewGuid().ToString()`).
3. Generate a deterministic genesis input for the Root. *(Note: The exact canonical value must be defined and proven via an MVP-3 test, e.g., a hash of a constant namespace).*
4. Construct `ChainRootInput` using the genesis hash.
5. Execute `ChainRootFactory.Create` and extract the REAL `ChainRootFingerprint`.
6. Construct `RegistryCertificateInput` using the real `ChainRootFingerprint` and the calculated `RegistryFingerprint`.
7. Execute `RegistryCertificateFactory.Create` to get the final `RegistryCertificate`.
8. Construct `CertificateChainEntry` with `Sequence = 1` and `Previous... = null`.
9. Invoke `RegistryCertificateChain.Register(entry)`.
10. Atomically serialize and save the `Entry` to `processed-runs.json`.

## 7. Required Tests
1. **Canonical Encoding Test:** Ensure `RunId` UTF-8 framing explicitly prevents boundary collision.
2. **Standard Emission:** End-to-end `TrustEmissionRequest` registers `Sequence 1`.
3. **Idempotency Test (MVP-2 Alive):** Duplicate `RunId` returns cached entry without re-invoking factories.
4. **Rehydration Test (MVP-2 Restarted):** Duplicate `RunId` with empty MVP-2 registry successfully rehydrates the exact original `CertificateChainEntry` into MVP-2.
5. **Healing Test (State Save Failure):** Emulating an un-persisted MVP-2 registration correctly heals disk state on retry.
6. **Bundle Collision Test:** Two different `RunId`s for the same `BundleId` — the second is strictly rejected.
