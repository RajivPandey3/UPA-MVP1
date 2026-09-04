# V1.1 Trust MCP Capability Proposal

## 1. STATUS
PROPOSED — NOT APPROVED

## 2. CONTEXT
REST Phase 2 is complete at `ce9cbf3`.
MCP infrastructure does not currently exist.

## 3. PURPOSE
Define a future MCP boundary that consumes the existing V1.1 trust capabilities without changing the V1.0 core.

## 4. ARCHITECTURE OPTIONS

We evaluate two primary architectural options for the MCP server:

### Option A: Standalone MCP Server / CLI using stdio
* **Deployment:** Distributed as a standalone executable CLI tool.
* **Process Model:** Child process spawned by the local agent.
* **Transport:** Standard Input / Standard Output (`stdio`).
* **Security Boundary:** Local machine execution context.
* **Local-agent Usability:** Excellent. Native integrations with local LLM agents rely heavily on `stdio`.
* **Operational Complexity:** Low. No network binding or TLS configuration required.
* **Coupling to ASP.NET:** None. Pure console application.
* **Testing:** Easily testable via standard IO redirection.
* **Future SDK Compatibility:** High, as the internal domain logic can easily be extracted into a shared client library.

### Option B: MCP Endpoint Embedded in Existing ASP.NET Application
* **Deployment:** Deployed as part of the existing REST API web server.
* **Process Model:** Long-running web server process.
* **Transport:** Server-Sent Events (SSE) over HTTP.
* **Security Boundary:** Network-level. Requires explicit authentication/authorization mechanisms.
* **Local-agent Usability:** Moderate. Requires local network access to the API and manual configuration of credentials.
* **Operational Complexity:** High. Requires securing the endpoint, managing HTTP connections, and handling CORS.
* **Coupling to ASP.NET:** High. Tightly bound to Kestrel and HTTP middleware.
* **Testing:** Requires full network integration testing.
* **Future SDK Compatibility:** Moderate.

**Recommendation (PROPOSED — NOT APPROVED):** **Option A (standalone stdio MCP server)** is the recommended path based on its superior local-agent usability and decoupling from ASP.NET middleware. However, this is strictly a proposal. No architecture is currently approved.

## 5. PROPOSED MCP TOOLS

### Tool: `emit_trust`
* **Purpose:** Emits a new trust certificate chain for a given artifact bundle.
* **Input Fields:** `run_id`, `artifact_bundle_id`, `artifact_hash`, `finalized_audit_snapshot`.
* **Output Shape:** Returns the newly created `certificate_chain` containing the V1.1 `CertificateChainEntry`.
* **Error Behavior:** Returns transport-level errors mapping to idempotency conflicts or bundle collisions.
* **Mapping:** Delegates directly to the V1.0 `TrustEmitter` capability mapped during Phase 2B.

### Tool: `verify_trust`
* **Purpose:** Verifies the continuity and identity of an existing certificate chain against an artifact.
* **Input Fields:** `artifact_bundle_id`, `artifact_hash`, `certificate_chain`.
* **Output Shape:** Returns `{"valid": true/false, "errors": [...]}`.
* **Error Behavior:** Returns `valid: false` with specific mismatch or sequence errors inside the `errors` array.
* **Mapping:** Delegates directly to the existing V1.1 `TrustVerificationService`.

### Tool: `inspect_trust`
* **Purpose:** Looks up a previously emitted certificate chain by its entry ID.
* **Input Fields:** `id` (the Entry ID string).
* **Output Shape:** Returns `{"id": "...", "status": "emitted", "certificate_chain": [...]}`.
* **Error Behavior:** Returns a specific not-found error if the trust entry does not exist.
* **Mapping:** Delegates directly to the existing V1.1 `TrustInspectionService`.

*(Note: No new trust semantics are invented here. These map exactly to existing capabilities.)*

## 6. CONTRACT MAPPING
MCP must preserve the approved V1.1 trust-contract semantics and use the established `snake_case` field names where applicable.
* Do not introduce new trust-domain semantics.
* Do not add speculative core trust fields.

## 7. ERROR MAPPING
Preserve established V1.1 error semantics. These are existing V1.1 error semantics being surfaced through the MCP boundary:
* `IDEMPOTENCY_CONFLICT`
* `BUNDLE_COLLISION`
* `TRUST_NOT_FOUND`
Do not invent additional trust-domain error codes unless clearly identified as transport-level proposal items requiring approval.

## 8. V1.0 SAFETY
* V1.0 trust core remains frozen.
* No V1.0 source changes.
* No duplicated trust logic.
* MCP is only a boundary adapter.

## 9. SECURITY
* `stdio` avoids network exposure and network transport authentication requirements.
* However, the invoking local process still operates under OS permissions.
* Access to the trust state/store must be explicitly considered.
* Authorization and process trust remain implementation/security questions requiring approval before any codebase changes are made.
*(This proposal does not invent credentials or security mechanisms as if they are already approved).*

## 10. TRANSPORT / SERVER DECISION
**Recommendation:** **A. standalone stdio MCP server**
*(This transport/server selection remains entirely inside the approval gate and is not an already-approved implementation fact).*

## 11. IMPLEMENTATION SCOPE
*PROPOSED Files / Projects (Do not create yet):*
* `src/UPA.TrustLayer.Mcp/UPA.TrustLayer.Mcp.csproj` (New standalone CLI project)
* `src/UPA.TrustLayer.Mcp/Program.cs`
* `src/UPA.TrustLayer.Mcp/Tools/EmitTrustTool.cs`
* `src/UPA.TrustLayer.Mcp/Tools/VerifyTrustTool.cs`
* `src/UPA.TrustLayer.Mcp/Tools/InspectTrustTool.cs`

## 12. NON-GOALS
* no V1.0 changes
* no REST contract changes
* no new trust semantics
* no speculative SDK
* no implementation in this phase
* no persistence changes
* no duplicated verification logic

## 13. APPROVAL GATE
PROPOSED — NOT APPROVED

Implementation must not begin until the MCP capability proposal and architecture choice are explicitly approved.
