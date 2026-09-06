# Performance Profiles

Every real-project proof records fixture bytes, build/scan milliseconds, and proof
status. Thresholds are gates, not estimates; a missing measurement fails the run.

| Profile | Fixture size | Time budget | Use |
|---|---:|---:|---|
| Small | <= 1 MB | <= 10 s | smoke and pull request proof |
| Medium | > 1 MB and <= 100 MB | <= 60 s | scheduled integration proof |
| Large | > 100 MB and <= 1 GB | <= 10 min | nightly proof |
| Stress | > 1 GB | declared per fixture | capacity and cancellation proof |

Each result must include the profile, exact fixture hash, machine/runtime details,
measurement, threshold, and pass/fail decision. Threshold changes require a
reviewed rationale and a new baseline; silent relaxation is not allowed.
