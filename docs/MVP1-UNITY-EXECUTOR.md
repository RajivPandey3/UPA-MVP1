# Unity Executor v1.0

## Allowlist

| Operation | MVP-1 |
|---|---|
| Create GameObject | Yes |
| Set Transform | Yes |
| Add Component | Yes, type-resolved and constrained |
| Set Tag | Yes |
| Set Layer | Yes |
| Save explicit target Scene | Yes |
| Delete arbitrary object | No |
| Delete assets | No |
| Modify project settings | No |
| Execute arbitrary C# | No |
| Run shell/process | No |
| Silent project-wide mutation | No |

## Rollback

Unity `Undo` is the primary rollback mechanism in this MVP.
The transaction creates an Undo group and reverts it if an operation fails.

## Important limitation

This executor currently finds scene objects by name. Production UPA should use
stable GlobalObjectId/ProjectModel IDs for exact targeting and add ambiguity checks
before allowing mutation.

## Next

Target Resolver + Unity Operation Library v1.1:
- GlobalObjectId targeting
- component property setting
- prefab-safe operations
- serialized-property transactions
- project settings allowlist
- stronger precondition contracts
- audit persistence
