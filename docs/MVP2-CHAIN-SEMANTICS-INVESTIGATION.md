# MVP-2 Chain Semantics Investigation

## Objective
To definitively answer the three core architectural questions raised during the MVP-3 recovery experiment, and to establish the true semantic intent of MVP-2's `RegistryCertificateChain`.

## 1. What is the intended semantic meaning of `BundleFingerprint`?
**Evidence from Source/Test:**
MVP-2's `Verify()` strictly enforces `current.BundleFingerprint == previous.BundleFingerprint` across the entire chain.
**Analysis:**
This proves that a "Chain" in MVP-2 is intrinsically bound to a **single, immutable entity**. The `BundleFingerprint` represents the cryptographic hash of that entity. In software supply chains, a "Bundle" typically refers to an immutable release artifact (e.g., a `.zip` file, a Docker image, or a specific finalized build output). It is **not** a project container for multiple different artifacts.

## 2. What entity does `BundleId` represent in MVP-2 design?
**Evidence from Source/Test:**
Because the `BundleFingerprint` must remain invariant for a given `BundleId`, `BundleId` cannot represent a mutable "Project" that builds multiple different artifacts over time. 
Instead, `BundleId` represents a **Specific Release Artifact / Build Entity**.
**The Semantic Model:**
When a pipeline runs, it produces a specific artifact. That artifact gets a unique `BundleId` and `BundleFingerprint`. 
The `Sequence` (1, 2, 3...) in MVP-2 is NOT meant to represent "Run 1, Run 2, Run 3" of a project. Instead, the sequence represents the **sequential accumulation of trust attestations for the SAME artifact**.
*Example:*
- `Sequence = 1`: Pipeline Audit Trail (Builder certification).
- `Sequence = 2`: QA Test Results (Testing certification).
- `Sequence = 3`: Manual Approval (Governance certification).
All these sequences belong to the same `BundleId` and share the same `BundleFingerprint`.

## 3. Is it possible to keep `RunId` as an entry-level identity while maintaining a stable bundle fingerprint?
**Evidence from Source/Test:**
Yes, it is not only possible, but it aligns perfectly with MVP-2's `RegistryCertificateInput`, which accepts:
- `TargetCertificateId` / `TargetCertificateFingerprint`
- `OrderedSubjectCertificateIds`
- `RegistryFingerprint`

**The Semantic Resolution (Option A/C Hybrid):**
MVP-3's role is to act as the "Builder certification".
- **`BundleId`:** Maps to the unique Artifact/Build produced by the pipeline.
- **`BundleFingerprint`:** The canonical hash of the produced artifact (e.g., `sha256sums.json` or the release `.zip`).
- **Execution Evidence (`RunId`):** The MVP-1 `AuditTrail` (which changes per run) is NOT the `BundleFingerprint`. It is the **Subject Evidence**. MVP-3 will hash the `AuditTrail` and map it to `TargetCertificateFingerprint` or `RegistryFingerprint` for that specific sequence entry.
- **Sequence Mapping:** Since MVP-3 only emits the *initial* execution proof for a newly built artifact, MVP-3's emission will almost always be `Sequence = 1`. Later sequences would be added by downstream QA/deployment gates, not by MVP-3.

## Conclusion & Architectural Recommendation
The hypothesis that MVP-2 chains pipeline runs chronologically for a project is incorrect. MVP-2 chains **trust attestations for a single immutable artifact**. 

Therefore, MVP-3 should adopt **Option A (One Run = One Bundle)** with a slight nuance:
- Every successful MVP-1 pipeline run produces a unique artifact (`BundleId`).
- MVP-3 emits `Sequence = 1` for that `BundleId`, embedding the MVP-1 `AuditTrail` hash as the trust evidence.
- This eliminates the need for massive multi-entry chain rehydration in MVP-3 (since MVP-3 only ever creates `Sequence 1`), and perfectly satisfies MVP-2's invariant rules without breaking idempotency.
