# Universal Quality Gates

The project treats a claim as valid only when an executable gate produces evidence.

## Required gates

| Area | Gate | Release rule |
|---|---|---|
| Build | Release solution build | Zero errors |
| Correctness | All test projects | Zero failures and zero skips |
| Security | Path, approval, traversal and overwrite tests | All pass |
| Verification | Expected output is independently observed | Evidence required |
| Recovery | Crash/rollback probe | Pass required |
| Performance | Small, large and stress profiles | Thresholds recorded |
| Compatibility | Adapter capability/version validation | Unsupported requests blocked |
| Auditability | Journal and TRX evidence | Artifacts retained |

## Performance profiles

`UPA_PERF_PROFILE=small` is the fast feedback profile. `large` is the release profile and
`stress` is the scheduled capacity profile. A timeout is a failure, never a pass.

## Safety rule

Tests may be separated by execution tier, but they may not be silently removed. Every
excluded tier must have a dedicated job and retained result artifact before release.
