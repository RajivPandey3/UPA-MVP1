# Advanced Operation Library v1.2

## Supported

### Scene/GameObject
- Tag
- Layer
- Rigidbody
- BoxCollider
- SphereCollider
- Material assignment
- Prefab save

### Asset
- ScriptableObject creation

## Explicitly not supported yet
- arbitrary ProjectSettings mutation
- arbitrary serialized property mutation
- automatic prefab asset propagation
- arbitrary asset deletion
- arbitrary shader graph generation
- arbitrary import-setting changes

The operation library is intentionally typed and narrow.

## Next
ProjectSettings Operation Library + typed import settings + prefab instance/asset
transaction semantics, followed by integration into the unified execution planner.
