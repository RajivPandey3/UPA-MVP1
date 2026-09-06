# Multi-platform direction

Status: first foundation implemented and tested; additional production platform adapters remain pending.

## Product boundary

UPA should coordinate explicitly supported project tasks across platforms. A platform is supported only when its advertised operations have executable adapters and independent acceptance evidence. Unknown projects and unsupported operations must be reported as unsupported, not routed to an unrestricted shell fallback.

The initial platform set is awaiting the user's selection. Software projects, game engines and general desktop automation are different scopes; the architecture below does not depend on choosing one of them yet.

## Current coupling

- `GovernedPipeline.ExecuteRun` constructs project, C# and assembly scanners directly.
- `UPA.Core.ScanResult` contains Unity version, render pipeline, scene and GameObject fields.
- `UpaPlan.UnityCreation` and the planning project's linked Unity command parser encode a specific platform operation.
- `OutputVerification` contains Unity YAML verification alongside generic text verification.
- `AdapterExecutor` is a fixed enum organized around Unity concepts.

Existing abstractions therefore provide a starting point, not proof of platform independence.

The first extraction is now available in `UPA.Workflows`: `IPlatformAdapter`, `WorkflowPlan`, `PreparedWorkflow`, `IVerifiedTransaction`, `OutputVerifierRegistry`, `RunJournal` and `WorkflowRunner`. The existing Unity path is wrapped as a Unity adapter, while a test-only text platform proves that the shared approval/audit/verification lifecycle can run without Unity-specific types.

## Target responsibilities

| Shared core | Platform integration |
| --- | --- |
| Project/workspace identity and permitted paths | Project detection and version compatibility |
| Typed operation graph and dependency checks | Domain-specific operation schemas and target resolution |
| Preview and approval bound to the complete operation content | Concrete change previews for that platform |
| Run identity, audit isolation and durable journal | Executor and platform-specific failure handling |
| Verification policy and evidence records | Independently selected output verifier |
| Recovery states and capability discovery | Recovery implementation for supported operations |

Platform facts belong in explicit platform models. Shared core types must not require a Unity scene, a C# assembly or a particular language runtime.

## Capability contract

Each registered capability needs a stable identifier and version, supported project/runtime versions, typed inputs, declared read/write scope, preconditions, approval requirements, execution entry point, expected-output schema, verifier, and recovery behavior. Availability is separate from successful execution.

The host chooses registered verifiers according to approved capability/output schemas. An executor does not certify itself by returning success text. Approvals bind the project identity, operation graph, parameters, target paths and preview version.

Project detection must allow multiple platforms in one repository. Ambiguous routing must be visible before planning; a web frontend and a Python service cannot silently be treated as a single Unity project.

## Migration order

1. Capture the current Unity workflow and refusal behavior as compatibility tests.
2. Extract platform-neutral workspace, operation, approval and evidence contracts.
3. Move Unity detection, typed commands and output checking behind a Unity integration.
4. Introduce capability registration and explicit routing with unsupported/ambiguous results.
5. Implement one selected non-Unity integration and its first complete operation.
6. Prove both integrations use the same approval, audit, failure and verification lifecycle.
7. Expand supported operations only after their own acceptance gates pass.

The first non-Unity proof is intentionally a small text artifact adapter. It is an architecture proof, not a claim that web, Python, Unreal, Godot or arbitrary desktop automation is already supported.

## Minimum multi-platform proof

- Unity integration still passes its existing end-to-end and refusal checks.
- A second integration performs a real operation in an isolated project and independently verifies the resulting artifact.
- Its project can be inspected and its workflow run without Unity installed or Unity-specific project metadata.
- The same cross-platform tests reject wrong-project approval, changed plans, fabricated success and unsupported capabilities.
- Failure/recovery behavior is tested for each integration; one platform's PASS does not transfer to another.
- CLI capability output distinguishes installed, available, unsupported and verified operation/version combinations.

General natural-language understanding, arbitrary desktop control and universal platform support remain separate claims requiring separate scope and evidence.
