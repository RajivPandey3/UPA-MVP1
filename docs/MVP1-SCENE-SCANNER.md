# Scene Scanner v1.0

## Implemented
- Scene asset discovery through AssetDatabase
- Additive read-only inspection of scene assets
- Scene hierarchy traversal
- Stable IDs using scene path + GlobalObjectId
- Transform snapshot
- Active state
- Tag/layer
- Component type discovery
- Missing script detection
- Prefab instance status
- Basic EditorWindow
- Editor test fixture

## Important design decision
The scanner restores the Editor SceneManager setup after temporary additive inspection.
It never calls `SaveScene` and does not intentionally modify project assets.

## Next layer
Prefab Scanner + deeper serialized-reference extraction.
