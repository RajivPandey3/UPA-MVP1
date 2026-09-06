# Release Evidence Checklist

A release is publishable only when every gate below has recorded evidence.

## Required gates

- [ ] Clean working tree: `git status --short` returns no product changes.
- [ ] Full verification passes: `./verify-mvp1.ps1 -ResultsPath verification/ci-evidence`.
- [ ] Dependency audit reports no High/Critical vulnerabilities.
- [ ] Release evidence files have SHA-256 entries and a deterministic manifest fingerprint.
- [ ] GitHub Quality Gate is successful for the exact release commit.
- [ ] Independent reviewer records approval and scope.
- [ ] Annotated Git tag points to the verified commit.
- [ ] Rollback target and recovery evidence are recorded before publication.

## Evidence record

Record the commit SHA, tag, CI run URL/ID, manifest fingerprint, reviewer, approval
timestamp, rollback target, and links to the retained evidence artifact. Do not mark
an unchecked item as complete or infer missing evidence.

## Failure rule

Any failed, missing, stale, or unknown gate blocks release. Fix the cause and rerun
the complete checklist against the new commit; never reuse evidence from another SHA.
