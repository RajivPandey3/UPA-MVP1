# MVP-1 v1.8 Integration Repair

Build feedback identified three categories of defects in the assembled tree.

## Repairs applied

1. Added a shared `tests/GlobalUsings.cs` with `global using Xunit;` so all
   xUnit test projects resolve `Fact`, `Assert`, and related test APIs.
2. Repaired the malformed multiline string literal in
   `UPA.Analysis.Tests/ProjectScannerTests.cs`.
3. Repaired the `UpaPlan` constructor call in `UPA.Planning/IntentPlanner.cs`.
   The plan model's `Executable` property remains explicitly `false`, preserving
   MVP-1's no-standing-execution-authority rule.

The source tree remains an integrated MVP-1 baseline; runtime verification must
still be performed with the Windows .NET 8 SDK.
