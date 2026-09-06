# Repair evidence — 2026-09-05

Final automatic run: **144/144 .NET tests passed across 15 suites, zero failed or skipped; all seven independent Unity regression checks passed; the complete supported Unity workflow passed.** Runner exit code: 0. Machine-readable summary: `fix-evidence/final/summary.json`. Saved integrated scene: `UnityOutsider/Assets/proof-045a9c5ad94f4d80ad64ed82cdcf0e70.unity`.

Build limitation: NuGet emitted NU1900 because vulnerability metadata could not be fetched for one test project. Compilation and tests succeeded; this run does not claim a completed dependency-vulnerability audit.

## 1. Planning restrictions and word boundaries

Before: six new regression cases failed (`fix-evidence/01-before/*.trx`): five restricted requests still generated mutation plans, and `address` triggered `add` inside a word.

After: all nine planning tests passed (`fix-evidence/01-after/*.trx`). Matching now uses word boundaries. Unsupported restrictions produce a blocking clarification requirement and no mutation actions. Mixed requests are conservatively blocked as a whole; selective execution of their positive clauses is not claimed.

## 2. Completion based on supplied flags

Before: the test requiring caller flags not to prove completion failed (`fix-evidence/02-before/*.trx`).

After: all four gate tests passed (`fix-evidence/02-after/*.trx`). The compatibility `Start` API blocks at the execution boundary instead of reporting a completed operation.

## 3. Actual orchestration and output verification

Added `GovernedPipeline.Execute`: actual project/script/assembly scans, model composition, health analysis, planning and validation precede binding and approval. Preconditions are checked again after approval. Execution must be followed by output verification; failures trigger rollback and block completion.

All eleven pipeline tests passed (`fix-evidence/03-after/*.trx`), including seven new cases: real file readback, no-op executor rejection, wrong-output rollback, rejected approval, approval for another plan, and two invalid intents that never reach binding.

The adapter interface is a trusted extension boundary, not a security boundary against malicious adapter code. It cannot prove that an intentionally dishonest verifier tells the truth.

## 4. Unity component catalog and validation

Before: the automatic Unity probe using `Rigidbody` exited 1 (`fix-evidence/04-before.log`). After the explicit type catalog, the same probe exited 0 (`fix-evidence/04-after.log`). Short, namespace-qualified and assembly-qualified names resolve to the same supported type; unknown names are rejected.

An additional negative test found dry-run success incorrectly reported despite validation errors (`fix-evidence/04-validation-before.log`). Success now requires zero errors. The final automatic Unity checks cover this case, aliases, approval, actual creation/component addition and preserving an earlier transaction during rollback. Each real executor call starts its own Unity Undo group.

## 5. Complete supported Unity workflow

`PipelineProbe` calls the production executing pipeline with `UnityBatchPlanBinder`. It first rejects approval and checks that no scene exists; next it supplies a negative intent and checks that no scene exists. It then approves the supported positive request.

The pipeline creates a new scene containing Player and Rigidbody. A second Unity Editor process reopens the saved scene and verifies its contents before completion. The first successful integration evidence is `UnityOutsider/pipeline-proof.json` (subsequent automatic runs refresh this file). The final runner also copies the report into its results directory.

Scope: one explicit command shape, a new scene, and supported component types. This is not arbitrary natural-language game creation or general editing of existing scenes. The full original Unity NUnit suite is separate; the runner executes the independent Unity regression probe and integrated workflow.

## 6. Portable dependency

Before: building the copied trust-emission project in `portable-check` failed because the sibling MVP2 dependency could not be resolved (`fix-evidence/06-before.log`).

After: the same copied project built with zero warnings and zero errors (`fix-evidence/06-after.log`). The trust-anchor source is bundled unchanged apart from line-ending normalization, with its provenance recorded. Build references now resolve within the checkout through `Directory.Build.targets`.

This verifies relocation on this laptop, not installation on every operating system or a completely new machine. .NET packages and a licensed Unity installation remain prerequisites.

## 7. Automatic sequential verification

`verify-mvp1.ps1` restores/builds, runs every .NET test project sequentially and stops on nonzero exit codes. It rejects failed/skipped test counters. With `-UnityPath`, it also runs the isolated Unity regressions and integrated workflow.

Failure propagation was verified using `verification/failing-dotnet.cmd`, which returns exit code 23. The script stopped at restore and returned failure (`fix-evidence/07-failfast.log`), rather than printing a verification success message.

The final run writes `fix-evidence/final/summary.json`, per-suite TRX files, Unity logs and the integrated scene proof. These execution records, rather than old release manifests, are the evidence for the repaired code.
