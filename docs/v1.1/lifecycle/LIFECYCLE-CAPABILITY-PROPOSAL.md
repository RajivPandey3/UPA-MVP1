# Phase 2R - Trust Lifecycle Capability Proposal

STATUS: PROPOSED — NOT APPROVED

## 1. Evidence / Current State
An investigation was conducted across the V1.0 trust-core implementation (`TrustEmitter.cs`, `DurableState`, `ProcessedRunRecord`) and the frozen V1.1 trust contract.

* **Observed Repository Evidence**: The V1.0 core relies on an append-only, immutable `Dictionary` mapping of `RunId` and `ArtifactBundleId` to `ProcessedRunRecord`. The `CertificateChainEntry` model contains no lifecycle status field (e.g., revoked, suspended). The REST inspection capability explicitly enforces a hardcoded `status = "emitted"`.
* **Capabilities Already Implemented**: `emit_trust`, `verify_trust`, and `inspect_trust` across REST, MCP, and SDK. These are not candidates for this phase.
* **Capabilities Explicitly Excluded by Existing Decisions**: A second trust-state store and any V1.0 persistence modifications are explicitly banned by existing roadmap and inspection decisions.

## 2. Problem Statement
The discovery task explored whether lifecycle management capabilities—such as revocation, certificate-chain rotation, or historical inspection—should be proposed as the next V1.1 capability. The core problem is evaluating whether these lifecycle concepts can be implemented without violating the strict V1.0 freeze constraints.

## 3. Candidate Capability
**Proposed Capability**: None.

**Conclusion**: There is no repository evidence to justify adding a lifecycle capability at this time.

* **Inferred Architectural Gap**: The system currently lacks any mechanism to invalidate, rotate, or revoke a previously emitted trust bundle.
* **Unresolved Questions**: If a bundle is later found to be malicious, how does the system signal consumers without revocation? This remains an unresolved governance question outside the current V1.1 scope.
* **Evidence that no additional capability is currently justified**: There are no roadmap documents requesting revocation, the V1.0 core explicitly omits status tracking, and adding status tracking would violate the strict prohibition against modifying the V1.0 core or creating a secondary state store.

## 4. Required Semantics
If a lifecycle capability like revocation were required, it would need new semantics to transition a trust record from `"emitted"` to `"revoked"`. However, because the V1.0 trust core lacks these semantics natively, implementing them would require either rewriting the V1.0 emission state machine or building a disjoint, secondary state store in V1.1. Both approaches are explicitly forbidden by existing governance rules.

## 5. Contract Impact
* **Capabilities Potentially Addable Without Changing Frozen Contract**: None identified in the lifecycle domain.
* **Capabilities Requiring Contract Review**: No lifecycle capability compatible with the currently frozen V1.1 contract was identified. Any future lifecycle capability would require formal contract-impact analysis and approval before implementation.

## 6. Persistence Impact
No lifecycle persistence design compatible with the currently approved constraints was identified. Any future lifecycle persistence approach would require explicit architectural review, including review of the V1.0 freeze and the existing prohibition on a second trust-state store.

## 7. V1.0 Safety Analysis
No lifecycle implementation is authorized by this proposal. Any future implementation must demonstrate that the V1.0 trust-core freeze and existing persistence constraints remain intact; modifying `TrustEmitter` or its persistence semantics is outside the currently approved scope.

## 8. REST/MCP/SDK Boundary Impact
Since no lifecycle capability is justified, there is zero impact on the existing REST, MCP, and SDK boundaries. The established operations (`emit_trust`, `verify_trust`, `inspect_trust`) remain complete and unchanged.

## 9. Security / Authorization Considerations
No capability is proposed, preventing the introduction of new authorization surfaces required for revocation or rotation.

## 10. Alternatives Considered
* **Secondary Lifecycle Database**: Considered mapping V1.1-specific revocation statuses in a separate persistence layer. This was rejected as it directly violates the explicit V1.1 architectural rule forbidding a "second trust-state store" as defined in `INSPECTION-CAPABILITY-DECISION.md`.
* **Inferred Rotation**: Considered proposing certificate-chain rotation, but there is no evidence of keys, signatures, or expiry bounds in the current implementation to necessitate rotation.

## 11. Explicit Non-Goals
* Do not define or implement revocation, rotation, or status transitions.
* Do not invent a secondary trust store to fake lifecycle states.
* Do not modify the frozen V1.1 contract to add lifecycle fields.
* Do not modify the V1.0 `TrustEmitter` or persistence logic.
* Do not create API routes or schemas for lifecycle operations.

## 12. Approval Gate
Based on this discovery, no lifecycle capability is proposed for implementation. The Trust Layer v1.1 will not incorporate revocation or rotation capabilities unless a future architectural mandate formally overrides the V1.0 freeze constraints. No implementation should proceed from this document.
