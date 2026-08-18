# Plan-to-Executor Adapter v1.5

## Purpose

Connect:

```text
Operation Registry
      ↓
Compiled Plan
      ↓
Typed Arguments
      ↓
Binding Catalog
      ↓
Preconditions
      ↓
Execution Batches
      ↓
Unity Executor
```

## Guarantees

- Unknown operation IDs are rejected.
- Missing required parameters are rejected.
- Type mismatches are rejected.
- Executor family is explicit.
- Operation-specific preconditions are attached.
- Execution remains disabled until the final approval/execution gate.
- Batching never bypasses dependencies or operation allowlists.

## Batch rule

Operations are grouped by executor family for transaction convenience.
This is not permission to reorder dependencies. The source plan remains authoritative.

## Next

Final MVP-1 integration:
`UPA Governed Build Pipeline v1.6`

It will combine:
- ProjectModel
- Health
- Intent
- Registry
- Plan compiler
- Validator
- Preview
- Approval
- Adapter
- Unity Executor
- Audit

into one end-to-end governed workflow.
