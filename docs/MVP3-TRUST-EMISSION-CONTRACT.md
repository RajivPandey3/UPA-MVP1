# MVP-3 Trust Emission Contract (Draft)

## 1. Identity & Semantic Model
- **`BundleId`**: Represents the **Project or Target identity**. It defines a distinct cryptographic chain.
- **`RunId`**: Represents the **individual execution/idempotency identity**. It maps 1-to-1 with a specific finalized execution.
- **`Sequence`**: A sequential 1-based index of executions **within a `BundleId`**.

## 2. Audit & Fingerprint Model
- **Input Constraint:** MVP-3 accepts only a **finalized AuditTrail snapshot** where no new events or timestamps will be appended.
- **Fingerprint Calculation:** The `BundleFingerprint` is calculated exactly once as `CanonicalHash.Sha256(FinalizedAuditTrail.ToText())`.
- **Idempotency Behavior:** If a `RunId` is retried, MVP-3 must return the already-emitted certificate without recalculating the hash or advancing the sequence.

## 3. Persistence & Recovery Model
- **Ephemeral Registry:** The MVP-2 registry (`RegistryCertificateChain`) is entirely in-memory and becomes empty upon restart. It acts as the **runtime trust registry**.
- **Durable State:** MVP-3 introduces a durable persistence store (e.g., `chain-state.json`) which is the **proposed restart-surviving source of continuity** when MVP-2 has restarted.
- **Authoritative Reconciliation:** Upon restart, MVP-3's proposed durable state acts as the continuity source. However, during runtime, MVP-3 must verify its durable state against the live MVP-2 registry. If MVP-3's durable state is corrupted but MVP-2 has the valid sequence in memory, MVP-3 must heal its durable state from MVP-2. A stale or corrupted durable state must never cause an invalid sequence to be pushed to MVP-2.

## 4. MVP-2 Construction/Registration Ordering (Algorithm)
- **Constraint:** Attempting to mutually reference `RegistryCertificateInput` and `ChainRootInput` for the same generation step creates a cryptographic circular dependency. MVP-2 natively requires `PreviousRegistryCertificateId` and `PreviousRegistryCertificateHash` to be `null` for `Sequence = 1`.
- **Construction Algorithm:** MVP-3 must follow this experimentally verified procedure to build objects:
  1. Allocate a distinct `RootId` and `RegistryCertificateId` upfront.
  2. For `Sequence = 1`, assert that there are no previous certificate IDs or hashes.
  3. Generate a deterministic bootstrap/genesis input mechanism to satisfy the first factory dependency.
  4. Create the `ChainRootCertificate` using the bootstrap input and obtain the actual `ChainRootFingerprint`.
  5. Create the `RegistryCertificate` by providing the newly obtained actual `ChainRootFingerprint`.
  6. Ensure the resulting objects strictly pass the existing MVP-2 validation rules.
  7. Register the finalized `CertificateChainEntry` in the MVP-2 registry.

## 5. Atomicity & Failure Semantics
- **No Cross-System Transactions:** MVP-2 provides no native transactional capabilities. Registration + local persistence must provide single-logical-operation semantics purely through MVP-3's idempotency and recovery logic.
- **State Save Fails Before Registration:** Safe to fail. Request can be retried.
- **Registration Succeeds but State Save Fails:** Critical failure. On retry, reconciliation must detect the successful registration in MVP-2 (if still in memory) or cleanly rebuild from durable state (if restarted), without duplicating the entry or breaking sequence.

## 6. Security/Governance Boundary
- **Evidence Only:** MVP-3 does not authorize executions or modify Unity state. It strictly translates execution history into immutable cryptographic evidence, preserving the MVP-2 boundary.

## 7. Testable Acceptance Criteria
- [ ] Concurrent requests with distinct `RunId`s for the same `BundleId` result in a perfectly sequential chain.
- [ ] Duplicate requests with the same `RunId` return the identical certificate without incrementing the sequence.
- [ ] MVP-3 strictly uses a finalized audit payload; the `BundleFingerprint` is stable across retries.
- [ ] Upon restart, MVP-3 successfully resumes the chain sequence based on its proposed durable state.
- [ ] If local state is corrupted while MVP-2 is running, MVP-3 heals local state from MVP-2 instead of emitting a broken sequence.
- [ ] The MVP-2 factory construction sequence must follow the verified bootstrap algorithm and produce objects that pass the existing MVP-2 validation rules without modifying MVP-2 source.
