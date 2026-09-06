# UPA MVP-1 Integrated v1.8

## CTO hardening (2026-09-06)

The current evidence and remaining acceptance gates are in [CTO-PROOF.md](verification/CTO-PROOF.md). Earlier proof reports are historical snapshots.

Supported typed commands now start with `Create`, `Make`, or `Add`; the rest of the command shape remains `a GameObject named Player with a Rigidbody in the scene.` Object names are parsed as data rather than scanned for extra commands. Mixed restrictions and unsupported requests remain blocked.

Approval tokens for `GovernedPipeline.Execute` must include the exact `ExecutionPreview.ContentHash` and a fresh issue time. The pipeline rechecks the bound plan after approval and independently reads approved output files; it no longer accepts an adapter's evidence strings as sufficient proof.

Interactive command-line entry point:

```powershell
D:\dotnet\dotnet.exe run --project src/UPA.Cli -- D:\6000.0.36f1\Editor\Unity.exe D:\Path\To\UnityProject Assets/NewPlayer.unity "Create a GameObject named Player with a Rigidbody in the scene."
```

Use forward slashes in the scene argument (`Assets/NewPlayer.unity`). Install the Unity executor Editor scripts into the target project first. The CLI displays the requested changes and requires the literal input `APPROVE`; default/EOF cancels. The isolated automated probe uses this same prompt with test input, not a real human approval session.

Durable run records live in the target project's `.upa/runs`. Inspect interrupted runs with:

```powershell
D:\dotnet\dotnet.exe run --project src/UPA.Cli -- inspect-runs D:\Path\To\UnityProject
```

Unity output is staged and verified before create-only publication. Failed rollback never deletes a published scene that another writer may have modified; such a run remains incomplete and requires review. This is conservative retention, not automatic recovery of every crash scenario.

## Verified repairs (2026-09-05)

See [repair evidence and limitations](verification/FIX-PROOF.md). The earlier release manifests and outsider review describe the previous baseline, not certification of these changes.

Run automatic verification, one test project at a time:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\verify-mvp1.ps1
```

Include the isolated Unity regression checks and complete request-to-saved-scene workflow:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\verify-mvp1.ps1 -UnityPath D:\6000.0.36f1\Editor\Unity.exe
```

The script stops on failure and writes TRX results, logs and a summary under `verification/fix-evidence/auto-*`. Unity tests deliberately approve changes only in `verification/UnityOutsider`. A licensed Unity installation and .NET 8 SDK are required; override the SDK executable with `-DotnetPath` if needed.

`GovernedPipeline.Execute` now scans, analyzes, plans, validates, binds a concrete transaction, requests approval, executes and verifies its output. `IPlanBinder` implementations are trusted application adapters responsible for accurate binding, preconditions and output verification. The old boolean-based `Start` API can evaluate gates but cannot report verified completion.

The included `UnityBatchPlanBinder` supports this exact command shape (case-insensitive):

```text
Create a GameObject named Player with a Rigidbody in the scene.
```

Supply a new Assets-relative scene path and install the `UPA.UnityExecutor` Editor scripts in the target project. The adapter creates a new scene, then starts a separate Unity process to reopen and verify it. It rejects existing scene paths and unsupported requests. It does not edit arbitrary existing scenes. An application must present the supplied `ExecutionPreview` and return a matching approval token only after user approval.

The planner conservatively requests clarification for restrictions such as `not`, `never`, `without`, and mixed positive/negative requests. It does not claim to understand every English or Hindi instruction. Component aliases are restricted to the explicit catalog in `UPAUnityExecutor.cs`; arbitrary custom component types are not accepted.

The previously external trust-anchor dependency is bundled under `dependencies/` with provenance. `Directory.Build.targets` redirects legacy project references to that local copy.

This is the consolidated integration workspace assembled from the uploaded MVP-1 milestones through the v1.8 release candidate.

## Verification

Use the Windows .NET 8 SDK environment. The expected local SDK path for this project is `D:\\dotnet\\dotnet.exe`.

1. `D:\\dotnet\\dotnet.exe restore .\UPA-MVP1.sln`
2. `D:\\dotnet\\dotnet.exe build .\UPA-MVP1.sln --configuration Release`
3. `D:\\dotnet\\dotnet.exe test .\UPA-MVP1.sln --configuration Release --no-build`

UnityPackage components require Unity Editor/package-manager verification separately.


FIXED4 integration repairs: class-level attribute capture in CSharpScanner, transitive operation dependency diagnostics, and natural-language material alias resolution.


FIXED9: Robust ordered-subsequence natural-language alias matching for multi-operation intents.
