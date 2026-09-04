# Phase 2Q — Native SDK Capability Proposal

**STATUS: PROPOSED — NOT APPROVED**

## 1. Problem Statement
While the V1.1 Trust Layer successfully provides machine-readable HTTP (REST) and JSON-RPC (MCP) boundaries, consumers (e.g., pipeline scripts, desktop applications, analysis tools) currently must construct raw HTTP requests, map JSON payloads to local types, and parse raw error codes. A native SDK solves this by providing typed, ergonomic client bindings, improving developer velocity and reducing integration errors.

## 2. Language Scope
The initial SDK proposal is **.NET 8 (C#) only**. Since the primary ecosystem of the UPA MVP is .NET, providing a native NuGet package immediately benefits internal consumers. Other language SDKs (Python, TypeScript) may be considered in future phases based on consumer demand.

## 3. Client Architecture
The SDK must be a **Thin REST/MCP Client**. 
It will simply act as a serialization and transport wrapper around `HttpClient` (or an MCP client). 
It **will not** be a "shared-domain" or "fat" SDK. It must not execute actual cryptographic verification or file mutations locally to prevent split-brain trust logic.

## 4. Operation Mapping
The SDK will surface an interface (e.g., `ITrustLayerClient`) strictly mirroring the approved operations:

* **emit_trust**: `Task<IReadOnlyList<CertificateChainEntry>> EmitTrustAsync(TrustEmitRequest request, CancellationToken ct);`
* **verify_trust**: `Task<TrustVerifyResponse> VerifyTrustAsync(TrustVerifyRequest request, CancellationToken ct);`
* **inspect_trust**: `Task<TrustInspectResponse> InspectTrustAsync(string entryId, CancellationToken ct);`

## 5. Preserving the Frozen V1.1 Contract
The SDK will define internal DTOs that exactly mirror the frozen V1.1 JSON `snake_case` contract (e.g., `artifact_bundle_id`, `certificate_chain`). By relying purely on these exact structures, the SDK introduces zero new trust semantics, effectively acting only as a strongly-typed proxy to the V1.1 API boundary.

## 6. Error Mapping
The SDK will transparently catch standard HTTP error responses (e.g., 409 Conflict, 404 Not Found) and map their body payloads back to typed .NET exceptions, preserving exactly:
* `IDEMPOTENCY_CONFLICT` -> `TrustIdempotencyConflictException`
* `BUNDLE_COLLISION` -> `TrustBundleCollisionException`
* `TRUST_NOT_FOUND` -> `TrustNotFoundException`

## 7. Versioning and Compatibility
* **Versioning:** The SDK package version will align with the API contract version (v1.1.x).
* **Compatibility:** The SDK will use permissive JSON deserialization (`System.Text.Json` ignoring unknown properties) to ensure forward compatibility with minor additive API changes.

## 8. Authentication and Authorization
The baseline SDK will assume ambient authentication (e.g., delegating to an injected `HttpClient` that already possesses Bearer tokens or default Windows credentials). Specific authz handling, token acquisition, or login mechanisms remain undecided and are explicitly out of scope for this base proposal.

## 9. Dependency and Packaging
* **Packaging:** Published as a standard NuGet package (e.g., `UPA.TrustLayer.Client`).
* **Dependencies:** Minimal dependency footprint. It will depend strictly on `System.Net.Http` and `System.Text.Json` to maximize compatibility and avoid dependency hell for downstream consumer projects.

## 10. Testing Strategy
* **Unit Tests:** Will utilize a mocked `HttpMessageHandler` to ensure correct serialization and error mapping without network I/O.
* **Integration Tests:** Will run against a live local instance of `UPA.TrustLayer.Api`.
* **Consumer Compatibility:** Verified using an external "ConsumerTest" scaffolding project to ensure the NuGet package installs and resolves correctly.

## 11. V1.0 Safety Constraints
To maintain the integrity of the frozen core, this SDK proposal mandates:
* **No V1.0 source changes:** The SDK strictly consumes the V1.1 HTTP boundary.
* **No duplicated TrustEmitter/verification logic:** The SDK contains no domain logic.
* **No second trust store:** The SDK retains zero local state.

## 12. Alternatives Considered
* **Shared-Domain SDK (Fat Client):** Rejected. Reusing the actual `UPA.MVP3.TrustEmission` assemblies on the client would duplicate logic and risk creating a decentralized second trust store, violating V1.0 constraints.
* **Auto-generated SDK (Swagger/NSwag):** Considered. While possible, manually crafting a thin client is proposed initially to guarantee precise adherence to the exact semantic error mapping (e.g., specific `IDEMPOTENCY_CONFLICT` exceptions) without bloated generated code.

## 13. Approval Gate
**STOP.** 
SDK implementation must not begin until a separate `SDK-CAPABILITY-DECISION.md` governance document explicitly approves this proposal.
