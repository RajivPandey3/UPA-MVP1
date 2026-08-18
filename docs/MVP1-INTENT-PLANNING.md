# Intent + Planning Engine v1.0

## Core contract

Natural language is not executed directly.

```text
User Intent
   ↓
Intent Grammar
   ↓
Typed Plan
   ↓
Dependencies
   ↓
Preconditions
   ↓
Unknowns
   ↓
Approval Gate
   ↓
Execution layer (future)
```

## Important safety rule

A plan can describe a mutation without being authorized to execute it.
MVP-1 always returns `Executable = false`.

## Placeholder policy

When exact production art/assets are unavailable, the plan may request a clearly
identified placeholder/generator step rather than silently inventing an exact final asset.

## Next

Plan Validator + Preview Engine: validate dependencies/preconditions, explain the plan
in human language, calculate risk/confidence, and produce an explicit approval packet.
