# CTO acceptance evidence — 2026-09-06

This report supersedes earlier proof summaries for the current working tree. A passing automated check applies only to its stated scope. Production readiness is not approved by these results alone.

Final results: **167/167 .NET tests passed (15 suites), seven Unity component checks passed, three real process-crash checks passed, and the hardened Unity workflow passed.** The source-only relocated solution and CLI both built successfully; the relocated solution reported zero warnings and zero errors. The isolated Unity workflow took **29.95 seconds** in the final run; this is a measurement, not an accepted production performance target.

Twelve newly added regression cases failed before their respective repairs. Before/after results, including the first unsuccessful rollback cleanup attempt, are preserved rather than overwritten. `cto-evidence/acceptance.json` separates automated passes from the remaining production acceptance gates.

## Gate matrix

| Gate | Evidence and current scope |
| --- | --- |
| 1. Validated plan / execution | Two regression tests initially failed when actions were removed. Typed creation data now comes from planning, and the binder validates required actions, targets and dependencies rather than reparsing the original sentence. `cto-evidence/01-before` and `01-after`. |
| 2. Independent verification | The fabricated-evidence regression initially failed. The pipeline now independently reads the approved text or Unity scene output, verifies scene object/component references and hashes the same bytes it checked. Adapter evidence strings are not sufficient. `02-before` and `02-after`. |
| 3. Rollback ownership | A failed executable launch followed by another writer's scene reproduced deletion of that writer's file. Staging and create-only publication now preserve that scene and its metadata. The first fix attempt also exposed a missing-directory cleanup error; both failure and passing retry logs are retained. `03-before`, `03-after`, `03-after-retry`. |
| 4. Approval | Stale approval and changed-preview regressions initially failed. Approval now includes the preview/output content hash, plan identity and fresh timestamp. A changed bound plan is rejected before mutation. Included in `02-before`, subsequent pipeline suites. |
| 5. Concurrent audit | Two concurrent runs reproduced duplicate completion events in a shared audit. Executing pipeline calls now have separate per-run state. `05-before` and `05-after`. This is not a claim that two Unity Editors can safely open the same project concurrently. |
| 6. Interrupted execution | Durable-record test initially failed. Flushed journals now record execution and verification stages. The recovery probe kills real child processes before a write, after a write and during verification; each is recognized as incomplete rather than completed. `06-before`, `06-after`, `crashes-*/results.json`, final `crash-recovery.log`. |
| 7. Supported language | Four regressions initially failed for Controller/Material as object names and Make/Add as verbs. A shared explicit parser now separates names from verbs and produces typed operations. `07-before` and `07-after`. Mixed restrictions and arbitrary natural language remain unsupported and blocked. |
| 8. User workflow | Interactive CLI and a shared approval prompt are implemented. Prompt tests and the Unity integration probe exercise cancel/approve behavior using automated input. **Actual human acceptance: NOT TESTED.** A human interaction is not fabricated by calling a test input human approval. |
| 9. Production performance | Isolated workflow elapsed time is recorded in `pipeline-proof.json`. **Production latency/memory acceptance: NOT TESTED**: no representative production project or agreed numerical limits were supplied. No performance improvement or universal latency guarantee is claimed. |
| 10. Clean environment | Source-only relocation is checked independently; bundled dependencies and the linked parser must resolve within that copy. **Fresh-machine/OS acceptance: NOT TESTED**: this environment has no Docker/Podman installation and the relocated build still uses this laptop's SDK/package environment. |

## Automatic verification

The runner builds, tests each .NET suite sequentially, builds the CLI, executes real process-crash probes, then runs the isolated Unity component checks and complete typed-plan workflow. It refuses stale TRX directories and missing test-result files. New runs use:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\verify-mvp1.ps1 -UnityPath D:\6000.0.36f1\Editor\Unity.exe
```

Final evidence is collected under `cto-evidence/final`: test TRX/logs, crash log, Unity checks, integrated scene proof and `summary.json`. `cto-evidence/source-manifest.json` records source hashes and the underlying Git commit; the implementation is an uncommitted working-tree change.

## Recovery semantics and limits

The controller writes `.upa/runs` before executing and before verifying. Interrupted or corrupt records require review. Reusing a run ID cannot overwrite the existing journal. This is detection and review, not automatic repair of arbitrary crashes or proof of power-loss durability on every filesystem.

Unity changes are first created and verified in a unique staging location. Publication cannot overwrite an existing scene. A published scene is retained if subsequent verification/rollback fails, because another writer may have changed it. The journal records an incomplete outcome instead of falsely reporting successful rollback. Existing recovery tools do not automatically remove all abandoned staging artifacts after a controller crash.

Adapters still execute as trusted application code in the same process. Independent output checking catches fabricated success for missing or incorrect approved output; it is not process isolation against a malicious plugin or proof that no other file was touched.

The existing Unity Editor test window is not the new CLI. The production interface added here is `src/UPA.Cli`; the automated integration input is explicitly marked as a test. The full historical Unity NUnit suite is not included in the seven-check independent probe.

NuGet previously reported NU1900 while fetching vulnerability metadata. Passing compilation and behavioral tests do not substitute for a completed dependency-vulnerability audit.
