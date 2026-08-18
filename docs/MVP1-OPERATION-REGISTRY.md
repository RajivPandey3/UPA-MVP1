# Operation Registry + Grammar Binding v1.4

## Canonical vocabulary

Every executable capability now has:
- stable operation ID
- human display name
- executor family
- risk level
- natural-language aliases
- typed parameters
- preconditions
- dependencies
- preview template
- dry-run capability
- approval requirement

## Why this matters

The AI/planning layer no longer needs to invent implementation-specific actions.

It can map:

```text
"player ko rigidbody do"
        ↓
component.add_rigidbody
```

and then use the operation metadata to build a governed plan.

## Dependency behavior

If an operation has dependencies that were not explicitly matched, the compiler
emits a warning rather than silently inserting mutation.

Future planner policy may automatically insert safe prerequisite operations,
but only as visible plan steps.

## Next

Plan-to-Executor Adapter v1.5:
- bind compiled operations to actual Unity executors
- parameter validation
- precondition adapters
- operation-specific previews
- transaction grouping
- approval packet integration
