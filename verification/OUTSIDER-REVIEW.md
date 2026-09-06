# Outsider verification — 2026-09-05

## Verdict

UPA contains working components, including real file mutations and real Unity object/component creation. This run does not establish a complete natural-language-to-Unity workflow or correctness for arbitrary requests. Two independent probes demonstrate why passing component tests alone is insufficient.

## Fresh execution results

- Ran `D:\dotnet\dotnet.exe test .\UPA-MVP1.sln --configuration Release --logger trx --results-directory .\verification\outsider-results`.
- Result: 131 passed, 0 failed, 0 skipped across 15 test assemblies; command exit code 0. TRX evidence is in `outsider-results/`.
- The build requires a sibling project under `D:\UPA-MVP2\artifacts\v20.0-final`; this checkout is not self-contained.
- Ran `D:\dotnet\dotnet.exe run --project verification/OutsiderProbe --configuration Release` from the repository root. Exit code 0. This probe records observations rather than treating all observations as passing assertions.
- Independently verified approved file creation by reading its actual content. Dry run and missing approval prevented writes. A deliberate duplicate-file failure rolled back the earlier write.
- Scanner identified the Unity version and scene/prefab paths in a small synthetic project. This is a limited fixture, not a large production-project benchmark.
- Probe evidence: `outsider-results/probe-ffa9e7816c27447f8a2a541f86d26775/observations.json`; created file: the same directory's `Assets/proof.txt`.

## Real Unity execution

Used Unity 6000.0.36f1 in batch mode with the isolated `UnityOutsider` project, an unchanged copy of the repository's Unity executor, and the independent `OutsiderUnityProbe.Run` entry point.

- Dry run did not create an object.
- An attempt without approval did not create an object.
- An approved call created `CreatedByUPA`.
- An approved call added a Rigidbody using `typeof(Rigidbody).AssemblyQualifiedName`.
- Saved scene inspection confirms both `m_Name: CreatedByUPA` and a `Rigidbody` record in `UnityOutsider/Assets/OutsiderProof.unity`.
- Final Unity process exit code: 0. Results: `UnityOutsider/outsider-unity-results.txt`; log: `outsider-results/unity-qualified-type.log`.
- An initial short component name, `UnityEngine.Rigidbody`, failed resolution. The full assembly-qualified name worked. This is an input-format limitation, not evidence that component addition is universally broken.
- The initial sandboxed Unity launch failed licensing; the approved run outside the sandbox resolved licensing and executed successfully.

This directly exercised the executor API with constructed operations and an explicit test approval token. It did not run a natural-language request through a complete integrated application. The repository's full Unity test suite was not run.

## Reproduced limitations

1. `IntentPlanner.BuildPlan` generated the same `inspect-scene` and `create-gameobject` actions for both `Create a GameObject in the scene.` and `Do not create a GameObject in the scene.` Neither had a blocking unknown. Negation is not handled in this example. Both plans still had `Executable = false`, so this probe did not cause an unwanted mutation.
2. `GovernedPipeline.Start` reported `Completed` for `This is nonsense and has no implementation` when all caller-supplied status flags were true. Inspection confirms that this method emits events based on booleans; it does not invoke the scanner, planner or executor. Its completion status is not independent proof that the requested work occurred.

## Scope

No production implementation was changed. Added only verification harnesses, an isolated Unity project, and evidence. Existing tests provide useful regression evidence, but no finite test run proves that an operation always works for every input. Advanced Unity operations, comprehensive security properties, large production projects, and the entire integrated user workflow remain outside this verification.
