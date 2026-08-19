# MVP-1 ↔ MVP-2 Trust Boundary Investigation

## 1. Observed Current Boundary

An investigation of the `UPA-MVP1` and `UPA-MVP2` source code reveals a clear functional separation:

**MVP-1 (Execution Truth)**
- Produces `PipelineEvent` and `AuditTrail` representing execution history.
- The `AuditTrail` outputs plain text representations of events (`ToText()`).
- Contains no cryptography, deterministic hashing, or sequence chaining.
- Explicitly handles execution authority, validation, and Unity state mutation.

**MVP-2 (Cryptographic Trust / Continuity)**
- Provides `VerificationTrustAnchor` featuring `ChainRootRegistry`, `RegistryCertificate`, and `RegistryCertificateChain`.
- Strictly maintains cryptographic evidence using SHA-256 (`CanonicalHash`), sequence tracking, and chain continuity.
- Explicitly operates under the governance boundary: `"Evidence/integrity verification only. No revocation, permits, execution authorization, or Unity mutation."`

**Current Status**: The two milestones are naturally compatible but completely isolated. There is no existing code that connects MVP-1's audit logs to MVP-2's cryptographic registries.

## 2. Missing Integration Contract

To bridge MVP-1 and MVP-2, the following capabilities are currently missing:

- **Canonical Fingerprinting**: MVP-1's `AuditTrail` must be converted into a deterministic representation, and its SHA-256 hash must be computed to serve as the `BundleFingerprint` required by MVP-2.
- **Sequence and Chain State Management**: MVP-1 does not track previous execution states. A mechanism is required to maintain the `Sequence` and `PreviousRegistryCertificateHash` across pipeline runs to satisfy MVP-2's continuity validation.
- **Persistence of Trust State**: A mechanism to store and recover the previous chain state (e.g., after a crash or restart) is required, which goes beyond the scope of a simple adapter.

## 3. Candidate MVP-3 Trust Emission Architecture

Instead of artificially merging MVP-1 and MVP-2 classes, the proposed architecture introduces MVP-3 as a **Trust Emission Layer**.

**Proposed Data Flow:**

```text
       [ MVP-1 ]
   Execution Truth
          │
          │ Audit Event (RunId, AuditTrail)
          ▼
       [ MVP-3 ]
   Trust Emission Layer
   (Canonicalization + Chain State Management)
          │
          │ BundleFingerprint + Previous Chain State
          ▼
       [ MVP-2 ]
   TrustAnchor
   (CertificateChainEntry / RegistryCertificate)
```

**Proposed Contract (Hypothetical):**

```csharp
TrustEmissionService.Emit(
    string RunId,
    string CanonicalAudit,
    PreviousTrustState state
)
```

**Architectural Benefits:**
- Preserves the frozen execution architecture of MVP-1.
- Maintains the strict evidence-only boundary of MVP-2.
- Integration occurs cleanly at an explicit adapter boundary (MVP-3), avoiding tightly coupled code modifications in existing milestones.
