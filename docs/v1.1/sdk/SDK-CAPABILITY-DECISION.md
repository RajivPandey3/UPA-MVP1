# V1.1 Native SDK Capability Decision

**STATUS: APPROVED — CAPABILITY DECISION**

## 1. Scope
* **Approve a first-party native .NET 8/C# SDK.**
* The SDK is a thin client boundary only.
* The SDK must not contain `TrustEmitter`, verification, certificate-chain, cryptographic, persistence, or other trust-domain logic.

## 2. Transport
* Select the **V1.1 REST API** as the baseline SDK transport.
* Do NOT make MCP stdio a required SDK transport.
* Do NOT implement an MCP client as part of this SDK capability unless separately approved later.
* The SDK acts as a consumer of the approved REST boundary rather than directly coupling consumers to MCP transport details.

## 3. Approved Operations
The SDK will exclusively support the following approved operations:
* `emit_trust`
* `verify_trust`
* `inspect_trust`

## 4. Contract Preservation
* Preserve the frozen V1.1 request/response JSON contract exactly.
* Preserve `snake_case` wire names for serialization.
* Do not introduce new trust semantics.
* Do not change the REST contract.
* Additive SDK convenience methods are allowed **only** when they map directly to existing approved REST semantics.

## 5. Error Mapping
* `IDEMPOTENCY_CONFLICT` -> typed SDK exception
* `BUNDLE_COLLISION` -> typed SDK exception
* `TRUST_NOT_FOUND` -> typed SDK exception
* Preserve the underlying HTTP/status semantics.
* Do not invent additional domain error codes.

## 6. Versioning
* SDK package follows the V1.1 API compatibility boundary.
* Clearly distinguish SDK package version from API contract version.
* Breaking changes to the external `/v1` contract require a new major external API version.
* Backward-compatible additive changes may remain within `/v1` subject to review/approval.
* The SDK must remain compatible with approved additive API changes.

## 7. Authentication
* Baseline SDK accepts an injected `HttpClient` or equivalent caller-supplied transport configuration.
* The SDK does not own login, token acquisition, credential storage, or authorization policy in the first implementation.
* Explicitly mark advanced authentication mechanisms as future/undecided rather than silently implementing them.

## 8. Packaging
* Approve a standalone SDK project/NuGet package, tentatively named `UPA.TrustLayer.Client`.
* Keep dependencies minimal.
* Do not reference or package V1.0 TrustEmitter assemblies into the SDK.

## 9. Testing
* Unit tests for serialization, HTTP behavior, response mapping, and typed error mapping.
* Integration tests against the existing V1.1 REST API.
* Consumer compatibility test using a real separate consumer project.
* Verify that the consumer can reference the SDK package and invoke all three approved operations.
* Include malformed/missing required input coverage where applicable.

## 10. V1.0 Safety Constraints
* No V1.0 source modifications.
* No duplicated V1.0 TrustEmitter or verification logic.
* No second trust store.
* No local durable trust state.
* No changes to frozen Trust Core contracts.
* No changes to existing REST or MCP semantics merely to accommodate the SDK.

## 11. Explicit Non-Goals
* Python SDK
* TypeScript SDK
* MCP client implementation
* shared-domain/fat SDK
* cryptographic verification inside the SDK
* local trust persistence
* speculative operations
* speculative authentication framework

## 12. Alternatives
* **Shared-domain/fat SDK:** Rejected to prevent split-brain trust evaluation.
* **MCP-native SDK:** Rejected for this phase because REST is the approved baseline client boundary.
* **Auto-generated SDK:** May be reconsidered later but is not required for this capability decision.

## 13. Implementation Gate
**This decision APPROVES the capability and architecture.** 
However, implementation must still be performed as a bounded implementation phase against this decision. Do not create implementation files as part of this capability decision task.
