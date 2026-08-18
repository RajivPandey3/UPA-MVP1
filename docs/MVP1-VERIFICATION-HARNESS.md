# MVP-1 Verification Harness v1.7

## Golden scenarios

The fixtures define stable expectations for:
- normal scene-building requests
- high-risk project-settings changes
- validation-only requests

## Governance regression matrix

The harness requires coverage for:

1. valid plan
2. invalid plan
3. exact target resolution
4. ambiguous target rejection
5. missing approval
6. valid approval
7. preview enforcement
8. missing parameter
9. type mismatch
10. executor allowlist
11. rollback
12. deterministic audit
13. high-risk settings
14. typed importer operation
15. health blocking
16. successful end-to-end flow

## Definition of green

A verification run is green only when:
- zero failures
- zero blocked verification cases

Skipped cases do not silently become passes.

## Next

MVP-1 Release Candidate v1.8:
- consolidate package versions
- add one-command verification
- produce machine-readable verification report
- create release manifest
- freeze governance contracts before MVP-2.
