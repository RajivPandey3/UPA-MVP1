# UPA MVP-1 Integrated v1.8

This is the consolidated integration workspace assembled from the uploaded MVP-1 milestones through the v1.8 release candidate.

## Verification

Use the Windows .NET 8 SDK environment. The expected local SDK path for this project is `D:\\dotnet\\dotnet.exe`.

1. `D:\\dotnet\\dotnet.exe restore .\UPA-MVP1.sln`
2. `D:\\dotnet\\dotnet.exe build .\UPA-MVP1.sln --configuration Release`
3. `D:\\dotnet\\dotnet.exe test .\UPA-MVP1.sln --configuration Release --no-build`

UnityPackage components require Unity Editor/package-manager verification separately.


FIXED4 integration repairs: class-level attribute capture in CSharpScanner, transitive operation dependency diagnostics, and natural-language material alias resolution.


FIXED9: Robust ordered-subsequence natural-language alias matching for multi-operation intents.
