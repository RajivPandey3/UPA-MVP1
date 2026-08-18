# Asset Scanner v1.0

Implemented:
- AssetDatabase asset discovery
- stable GUID-based identity
- main object type
- importer type
- file size
- texture dimensions
- material shader name
- asset classification
- direct AssetDatabase dependencies
- unresolved dependency diagnostics
- read-only EditorWindow
- Editor test fixture

Known limitation:
Importer-specific deep settings are intentionally not mutated or normalized yet.
The next layers can add typed metadata readers for textures, models, audio,
materials, animations, shaders and ScriptableObjects.
