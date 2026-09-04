# V1.1 Trust MCP Capability Decision

## 1. STATUS
APPROVED — CAPABILITY DECISION

*This document approves the MCP capability direction and implementation scope. It does not authorize unrestricted future changes or ad-hoc architecture deviations.*

## 2. BASIS
This decision is explicitly based on the following repository evidence and constraints:
* **Current Checkpoint:** `ce9cbf3`
* **Completed REST Trust Layer:** The Phase 2 REST API boundary successfully validated the V1.1 domain services.
* **Reviewed MCP Capability Proposal:** The formal proposal evaluating `stdio` vs embedded architectures.
* **Absence of MCP Infrastructure:** Acknowledging that no MCP code or tooling currently exists in the repository.
* **Frozen V1.1 Contract:** The established, reviewed trust interface schemas.
* **V1.0 Trust-Core Freeze:** The absolute prohibition on modifying existing V1.0 core trust logic.

## 3. APPROVED CAPABILITY
This decision approves a V1.1 MCP boundary exposing the following existing trust capabilities:
* `emit_trust`
* `verify_trust`
* `inspect_trust`

These are strictly approved as **boundary adapters** layered over the existing V1.1 domain services and V1.0-backed core capabilities. **No new trust-domain semantics are approved.**

## 4. APPROVED ARCHITECTURE
**Approved:** Standalone MCP server using `stdio`.

Explicit restrictions:
* This is the explicitly approved architecture for Phase 2P.
* It is exclusively a boundary/interface layer.
* It does not replace the existing REST API.
* It does not modify V1.0 trust-core behavior.

## 5. TOOL CONTRACT
For each tool, the approved interface must preserve established `snake_case` names where applicable. Do not invent new fields.

**`emit_trust`**
* `run_id`
* `artifact_bundle_id`
* `artifact_hash`
* `finalized_audit_snapshot`

**`verify_trust`**
* `artifact_bundle_id`
* `artifact_hash`
* `certificate_chain`

**`inspect_trust`**
* `id` / `entry_id` lookup as established by the existing inspection capability.

## 6. OUTPUTS
Outputs must remain perfectly consistent with the existing REST/domain capabilities.

* **`verify_trust`** must preserve:
  * `valid`
  * `errors`
* **`inspect_trust`** must preserve:
  * `id`
  * `status` (value must be `"emitted"` for an existing emitted record)
  * `certificate_chain`
* **`emit_trust`** must expose the existing emitted `certificate_chain` result without introducing new trust semantics.

## 7. ERROR SEMANTICS
The implementation must preserve the following existing V1.1 error concepts:
* `IDEMPOTENCY_CONFLICT`
* `BUNDLE_COLLISION`
* `TRUST_NOT_FOUND`

**Do not create new trust-domain error codes.** Transport or protocol-level errors may be handled separately at the MCP JSON-RPC boundary, but they must not alter or obscure underlying trust-domain semantics.

## 8. V1.0 SAFETY
This decision explicitly approves **only an adapter/boundary implementation**.
The implementation MUST strictly require:
* No V1.0 source modifications.
* No V1.0 semantic changes.
* No duplicated trust verification algorithms.
* No second trust-state store.
* No new persistence semantics.
* Maximal reuse of existing V1.1 services and adapters wherever appropriate.

## 9. SECURITY
The following implementation requirements and constraints are formally recorded:
* The `stdio` transport avoids network exposure and HTTP authentication overhead.
* The local process still operates under the OS permissions of the invoking agent.
* Trust-state file access must be explicitly controlled and handled by the underlying OS environment.
* Process trust and authorization must be addressed practically during implementation and testing.
* **No new credentials or authentication mechanisms are being invented or approved by this decision.**

## 10. IMPLEMENTATION SCOPE
This decision approves only the minimum MCP project required to expose the three tools.

The following structure is approved as an implementation target (these files do not currently exist):
* `src\UPA.TrustLayer.Mcp`
* `src\UPA.TrustLayer.Mcp\Program.cs`
* `src\UPA.TrustLayer.Mcp\Tools\EmitTrustTool.cs`
* `src\UPA.TrustLayer.Mcp\Tools\VerifyTrustTool.cs`
* `src/UPA.TrustLayer.Mcp\Tools\InspectTrustTool.cs`

*Do not approve or implement unrelated SDK work under this scope.*

## 11. NON-GOALS
The following are explicitly excluded from this phase:
* REST API redesign.
* V1.0 core modification.
* New trust semantics.
* New persistence layers.
* SDK implementation.
* Speculative MCP tools beyond the approved three.
* Alternate MCP transports (e.g., SSE).
* Unrelated refactoring.

## 12. IMPLEMENTATION GATE
The V1.1 MCP boundary capability and standalone `stdio` architecture are now **APPROVED** for implementation.

Implementation must remain strictly within the approved scope defined above and must successfully pass:
* Unit tests
* Integration/API or MCP tests as appropriate
* Release build
* V1.0 core diff check
* `git diff --check`
