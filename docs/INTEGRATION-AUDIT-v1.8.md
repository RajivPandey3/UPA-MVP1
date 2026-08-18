# MVP-1 v1.8 Integration Audit

This workspace consolidates the uploaded MVP-1 milestones into one source tree.
Historical ZIPs remain separate source artifacts; this tree is the integration workspace.

Known merge points requiring explicit resolution:
- UPA.Core Contracts.cs evolved across scaffold/project/assembly/C# milestones.
- UPA.Analysis ProjectScanner.cs evolved from scaffold to project scanner.
- UPA.Analysis and UPA.ProjectModel target frameworks evolved to net8.0.

Next verification: build all .NET projects and run all test projects in a .NET 8 environment.
UnityPackage folders remain Unity Editor integration packages and require Unity Editor tests separately.

## Initial integration fixes applied

1. Consolidated duplicate `UPA.Core` contracts into one `Contracts.cs`.
2. Preserved deterministic `EntityId.FromStableKey` and added `EntityId.New` for legacy tests.
3. Added `IProjectScanner` and `ScanResult` to the canonical core contracts.
4. Upgraded `UPA.ProjectModel` to net8.0 and restored its `UPA.Core` project reference.
5. Upgraded `UPA.Analysis` to net8.0 with `UPA.Core` and `UPA.ProjectModel` references.
6. Changed the project scanner from random project IDs to stable project-root IDs.
7. Corrected the invalid backslash character literal in the assembly scanner source.
8. Added the missing `UPA.Operations` project reference to `UPA.Adapter`.

These changes are integration repairs, not new product capabilities.
Runtime build/test must still be performed on the Windows .NET 8 SDK environment.
