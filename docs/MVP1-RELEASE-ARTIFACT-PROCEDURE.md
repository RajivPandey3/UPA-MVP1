# MVP-1 Release Artifact Procedure

## Purpose

This document defines the release procedure for MVP-1 source verification, Unity verification, artifact creation, integrity checking, and release provenance.

## Source Freeze

- Identify the exact release source commit.
- Treat the `v1.8-final` tag as immutable.
- Do not move or recreate the release tag.
- Post-release verification metadata must not be represented as source changes to the frozen implementation baseline.

## .NET Verification

- Restore, build, and test the MVP-1 .NET solution in the required .NET environment.
- Record the build and test results.
- A failed or unavailable verification run must not be represented as a PASS.

## Unity Editor Verification

- Run the Unity Editor tests separately from the .NET verification.
- Record the Unity version/project used for verification.
- Record total, passed, failed, and skipped test counts.
- A skipped or unavailable Unity test is not a PASS.
- If verification is performed in a separate verification copy, identify that copy explicitly.

For the current MVP-1 verification evidence, the Unity EditMode result is 13 total, 13 passed, 0 failed, and 0 skipped.

## Artifact Creation

- Release artifacts are external to the Git-tracked source tree unless explicitly committed.
- Record the exact source commit associated with the artifact.
- Record the artifact filename and file count.
- Do not overwrite historical release artifacts.
- Create a new artifact when a release artifact must be regenerated.

## SHA-256 and Integrity

- Calculate the SHA-256 of the completed artifact.
- Record the artifact hash in the release verification record.
- Maintain hashes for the documented verification files in `sha256sums.json`.
- Recalculate affected hashes whenever documented files change.
- Do not claim an artifact hash until the final artifact bytes are fixed.

## Artifact Provenance

Artifact provenance must be recorded at release time because release ZIPs are external to Git.

The provenance record should identify:

- source commit;
- artifact filename;
- artifact SHA-256;
- artifact file count;
- verification results;
- release environment/tooling;
- creation date/time.

An artifact must not be described as reproducibly generated from Git history unless its generation provenance was actually recorded.

## Embedded Metadata Consistency

Before final release:

- Compare embedded `release-manifest.json` with the release verification record.
- Compare embedded verification metadata with the declared release state.
- Preserve historical RC verification records as historical records.
- Do not overwrite historical RC evidence merely to make it appear to be final-release evidence.
- Verify that hashes correspond to the exact final file contents.

## Final Release Status

Source verification, Unity verification, artifact integrity, and integration verification are separate claims.

A release must not be described as fully verified when a required verification category remains unverified.

For MVP-1, MVP1-to-MVP2 integration remains a separate verification state and must not be inferred from the Unity or .NET test results.

## Historical Artifact Handling

Historical artifacts remain separate source artifacts.

Do not overwrite, mutate, or silently replace a historical ZIP.

If a corrected or newly synchronized artifact is produced, preserve the historical artifact and assign the new artifact its own recorded provenance and SHA-256.
