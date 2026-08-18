# Plan Validator + Preview Engine v1.0

Pipeline:

```text
Intent
  ↓
Plan
  ↓
Validate
  ├── IDs
  ├── dependencies
  ├── ordering
  ├── cycles
  ├── preconditions
  ├── confidence
  └── blocking unknowns
  ↓
Preview
  ├── intended operations
  ├── risk
  ├── confidence
  └── approval requirements
  ↓
Approval Packet
```

The packet is NOT an execution token.

Next milestone:
Execution Sandbox + Transaction Engine v1.0, initially supporting only a tiny
allowlisted mutation surface with dry-run, rollback, audit log and explicit approval.
