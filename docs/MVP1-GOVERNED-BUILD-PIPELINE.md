# Governed Build Pipeline v1.6

## End-to-end lifecycle

```text
USER INTENT
    ↓
PROJECTMODEL
    ↓
HEALTH
    ↓
PLAN
    ↓
VALIDATE
    ↓
PREVIEW
    ↓
EXPLICIT APPROVAL
    ↓
OPERATION BINDING
    ↓
ALLOWLISTED UNITY EXECUTOR
    ↓
TRANSACTION / UNDO
    ↓
AUDIT
```

## Gate semantics

- ProjectModel missing → block
- Health failure → block
- Invalid plan → block
- Preview not accepted → block
- No explicit approval → wait
- Adapter rejection → block
- Executor failure → rollback/block
- Successful run → audit + completion

## Important

`ExecutionAuthorized` remains false at the pipeline state level. A real execution
attempt is authorized only for the specific approved transaction and only through
the existing executor controls.

This prevents a previous successful run from becoming a standing permission.

## MVP-1 status

The major governance architecture is now integrated conceptually:

1. Discovery
2. Unified model
3. Health analysis
4. Intent/grammar
5. Operation registry
6. Planning
7. Validation
8. Preview
9. Approval
10. Adapter
11. Unity execution
12. Transaction/Undo
13. Audit

## Next

MVP-1 Verification Harness v1.7:
- end-to-end scenario tests
- golden-plan fixtures
- approval-boundary tests
- ambiguity tests
- rollback tests
- deterministic audit checks
- regression matrix
