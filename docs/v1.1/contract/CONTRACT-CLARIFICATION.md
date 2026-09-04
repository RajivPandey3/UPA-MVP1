# V1.1 Trust Contract Clarification

STATUS: APPROVED - DOCUMENTATION CLARIFICATION

## 1. Discrepancy Identification
A pre-existing documentation discrepancy was identified between the frozen V1.1 JSON Schema (`UPA-TRUST-CONTRACT-V1.1-DRAFT.schema.json`) and the actual implementation of the REST API boundary (`TrustEmitRequest.cs`).

*   **Frozen Schema:** The `TrustEmissionRequest` definition omits the `certificate_chain` property and uses `additionalProperties: false`.
*   **Runtime Implementation:** The implemented V1.1 REST API, MCP Tools, and Native SDK explicitly require `certificate_chain` on the `TrustEmitRequest` DTO as part of the Phase 2 implementation.

## 2. Authoritative Contract
Following the absolute freeze rule, historical frozen documents are preserved unchanged to maintain the audit trail.

**The runtime implementation is the authoritative API contract.**

Consumers of the V1.1 API, MCP server, and SDK MUST provide an empty or populated `certificate_chain` array on the `TrustEmitRequest` (as enforced by the JSON bindings). 

## 3. Rationale for Preservation
Changing the frozen schema retroactively rewrites history, violating V1.1 governance. Changing the implementation to drop `certificate_chain` breaks the currently validated and released V1.1 endpoints. Therefore, the discrepancy is preserved in the documentation, with this document serving as the binding clarification.
