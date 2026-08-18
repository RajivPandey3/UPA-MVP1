# Target Resolver + Operation Library v1.1

## Target policy

1. Prefer GlobalObjectId.
2. Fallback to name only when exactly one match exists.
3. Multiple name matches = hard ambiguity error.
4. No silent nearest-object selection.
5. Scene must be loaded and explicitly targeted.

## Operation library

Currently allowlisted:
- Transform scalar properties
- primitive serialized component properties
- Add Component
- Tag
- Layer

Complex serialized types are intentionally rejected until typed handlers exist.

## Prefab rule

Prefab instance handling is not silently flattened. Future prefab-aware operations
must detect instance boundaries and explicitly declare whether they affect:
- instance only
- prefab asset
- nested prefab

## Next

Project Settings + Asset/Prefab Operation Library v1.2 with typed handlers,
exact asset IDs, prefab-aware transactions and stronger precondition contracts.
