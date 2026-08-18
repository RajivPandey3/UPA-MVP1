# Prefab Scanner v1.0

Implemented:
- Prefab asset discovery
- Temporary prefab-content loading
- Full prefab hierarchy traversal
- Transform/state/tag/layer snapshot
- Component discovery
- Missing-script detection
- Nested prefab dependency discovery
- Stable object IDs
- Read-only EditorWindow
- Editor test fixture

Safety:
`LoadPrefabContents` is paired with `UnloadPrefabContents`; the scanner never calls `SaveAsPrefabAsset` in production scanning code.
