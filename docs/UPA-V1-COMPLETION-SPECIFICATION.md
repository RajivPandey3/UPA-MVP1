# UPA V1.0 Completion Specification

## 1. Mission / Architectural Purpose
UPA is a **general-purpose governed automation and provenance architecture**. Its objective is to provide strict governance, deterministic automation, validation, and traceability to the lifecycle of *any* applicable project or system.

While the current implementation domain (MVP-1) targets Unity projects, the fundamental UPA principles are project-agnostic. It enforces that changes are governed and approved, automation rejects bad inputs, and execution leaves a verifiable cryptographic audit trail.

## 2. Current Committed Capabilities
The architecture is currently realized through the following milestones:
* **MVP-1 (Execution, Governance, Audit):** Intent planning, health analysis, validation gates, preview, explicit approval, and atomic execution resulting in a deterministic `AuditTrail`.
* **MVP-3 (Trust Emission):** Deterministic encoding and emission of the MVP-1 `AuditTrail` into cryptographic evidence.
* **MVP-2 (Cryptographic Trust Anchor):** The immutable ledger (`RegistryCertificateChain`) that securely stores and links execution evidence sequentially.

## 3. Required End-to-End Outcome
A complete UPA V1.0 system MUST allow an authorized entity to:
1. Propose a change to a target system.
2. Pass the change through automated health and validation gates.
3. Require explicit approval for the planned execution.
4. Execute the change atomically, producing a reliable audit log.
5. Cryptographically attest that log into an immutable trust anchor.

*(Note: The current integrated pipeline of MVP-1 → MVP-3 → MVP-2 successfully implements this exact outcome at the API/library level).*

## 4. Completion Criteria (Definition of Done)
To formally declare **UPA V1.0 COMPLETE**, the following must be true:
1. The end-to-end outcome (Section 3) is programmatically verified and unbroken. (Currently: **PASS**)
2. **[OPEN]** DECISION-01 must be resolved.
3. **[OPEN]** DECISION-02 must be resolved.

## 5. Explicit Non-Goals
To prevent scope creep, the following are explicitly **NOT REQUIRED** for V1.0 unless mandated by the resolution of open decisions:
* **Remote / Distributed Ledger:** A local file-based JSON registry is sufficient for V1.0. Network synchronization is a deployment/infrastructure concern, not a core architectural requirement.
* **CLI / Custom UI:** Unless deemed critical for the V1.0 user workflow, the system is considered complete at the architectural API/SDK layer.
* **Artifact Packaging:** Binding certificates to physical release packages (e.g., ZIP, `.unitypackage`) is a distribution mechanism implementation, not the core trust architecture.
* **MVP-4:** Not assumed to exist. Development remains frozen unless closing the Completion Criteria explicitly necessitates a new implementation milestone.

## 6. Open Decisions
These genuinely unknown variables must be decided before declaring V1.0 complete:

* **DECISION-01:** Does UPA V1.0 require an explicit "Verification Interface" (a way for an external consumer to validate an artifact against the MVP-2 registry) to be considered feature-complete?
* **DECISION-02:** Does UPA V1.0 require a specific "Triggering Interface" (CLI or Editor extension) for human interaction, or do we deliver V1.0 strictly as a foundational SDK/Library?
