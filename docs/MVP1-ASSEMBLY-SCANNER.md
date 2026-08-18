# Assembly Scanner v1.0

Implemented:
- `.asmdef` discovery
- assembly identity
- references
- unresolved-reference diagnostics
- optional Unity references
- platform include/exclude lists
- define constraints
- version define entries
- autoReferenced
- noEngineReferences
- overrideReferences
- testAssemblies
- assembly dependency edges
- basic script ownership detection

Known limitation:
Unity's full compilation graph and package-provided assemblies are not completely reproduced by filesystem-only scanning.
The Unity Editor adapter will later enrich this model using Unity's compilation pipeline APIs.
