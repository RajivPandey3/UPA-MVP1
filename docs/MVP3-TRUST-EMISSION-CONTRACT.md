# MVP-3 Trust Emission Contract (Draft)

## 1. Scope & Responsibility
MVP-3 introduces the **Trust Emission Layer**. Its primary responsibility is to act as a resilient orchestrator between MVP-1 (Execution Truth) and MVP-2 (Cryptographic Trust). It translates plain audit trails into cryptographically chained certificates while ensuring strict atomicity, state persistence, and crash recovery.

## 2. Inputs/Outputs
- **Input:** `TrustEmissionRequest` containing `RunId` (string) and `AuditTrail` (plain text) from MVP-1.
- **Output:** A finalized `CertificateChainEntry` registered in MVP-2, alongside a durably persisted local chain state.

## 3. Idempotency — RunId
- **Requirement:** A `RunId` must be idempotently resolved to at most one accepted certificate-chain entry.
- **Behavior:** If a request with an already processed `RunId` is received, retries must return/recover the existing accepted entry rather than create another entry.

## 4. Canonical Fingerprint
- **Requirement:** MVP-3 must predictably hash the MVP-1 `AuditTrail`.
- **Behavior:** The `BundleFingerprint` must be generated using `CanonicalHash.Sha256(AuditTrail.ToText())` exactly as expected by MVP-2.

## 5. Sequence & Previous Certificate
- **Requirement:** Cryptographic continuity must be maintained.
- **Behavior:** MVP-2 registry is the authoritative source for accepted chain state; local state is a recoverable cache/persistence aid. MVP-3 must fetch the last known `RegistryCertificateHash` and `Sequence` (N), and correctly assign `Sequence = N + 1` with the respective previous hashes for the new entry.

## 6. Registration/State-Update Atomicity
- **Requirement:** Registration + local persistence must provide single-logical-operation semantics through idempotency, reconciliation, and recovery; no cross-system atomic transaction is assumed.
- **Behavior:** A certificate cannot be considered successfully emitted until its state is durably persisted. However, MVP-3 must never write a local state that contradicts the actual accepted state inside the MVP-2 registry.

## 7. Crash Recovery
- **Requirement:** The system must survive unexpected terminations during emission.
- **Behavior:** Upon restart, MVP-3 must safely determine if a pending certificate was registered in MVP-2 before the crash, and heal its local state accordingly without breaking the chain.

## 8. Concurrency
- **Requirement:** The chain must remain strictly sequential even under concurrent pipeline runs.
- **Behavior:** MVP-3 must serialize emission requests to prevent sequence collisions and race conditions during sequence allocation.

## 9. Registry ↔ Local State Reconciliation
- **Requirement:** Local persistence must always reflect the truth of MVP-2.
- **Behavior:** If the local state falls behind or becomes corrupted, MVP-3 must query MVP-2's `RegistryCertificateChain` to rebuild its local persistent state before processing new requests.

## 10. Failure Semantics
- **State Save Fails Before Registration:** Safe to fail. Request can be retried.
- **Registration Fails:** Safe to fail. Request can be retried.
- **Registration Succeeds but State Save Fails:** Critical failure. On retry, reconciliation must detect the successful registration and heal the state without duplicating the entry.
- **Invariant:** MVP-3 will never push a sequence that breaks MVP-2's `Verify()` rules.

## 11. Security/Governance Boundary
- **Requirement:** MVP-3 is evidence-only.
- **Behavior:** MVP-3 does not authorize executions or modify Unity state. It strictly translates execution history into immutable cryptographic evidence, preserving the MVP-2 boundary.

## 12. Testable Acceptance Criteria
- [ ] Concurrent requests with distinct `RunId`s result in a perfectly sequential chain.
- [ ] Duplicate requests with the same `RunId` return the same certificate without incrementing the sequence.
- [ ] Simulating a crash between registration and state-save automatically recovers on the next request.
- [ ] Corrupted local state is successfully healed from MVP-2 registry.
- [ ] After a crash/restart, MVP-3 reconciles against the authoritative MVP-2 chain and never creates a duplicate certificate for an already accepted `RunId`.
- [ ] A stale or corrupted local state cannot cause an invalid sequence or `PreviousRegistryCertificateHash` to be emitted.
- [ ] MVP-2 source code remains entirely unmodified (no transactional logic added to MVP-2).
