# ProjectSettings + Import Settings Operation Library v1.3

## Project settings
Typed operations include:
- Physics.gravity
- Time.fixedDeltaTime
- PlayerSettings.productName
- PlayerSettings.companyName
- QualitySettings.vSyncCount
- QualitySettings.antiAliasing
- QualitySettings.shadowDistance

## Importers
Typed operations include:
- Texture max size
- Texture compression policy
- Model material import mode
- Model animation type
- Audio load-in-background

## Governance
No arbitrary SerializedProperty path is accepted.
No raw ProjectSettings asset editing is exposed.
Importer changes are explicit, typed and followed by `SaveAndReimport`.

## Important
Exact Unity API availability can vary by Unity version and render pipeline.
The package targets the Unity 2021.3+ API family and should be compiled against
the project's actual Unity version before production use.

## Next
Operation registry + executor integration:
- register every operation with grammar metadata
- map natural-language intent to typed operations
- precondition templates
- risk metadata
- dry-run preview
- approval packet
- audit persistence
