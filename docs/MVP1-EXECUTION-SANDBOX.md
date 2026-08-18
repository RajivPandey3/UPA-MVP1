# Execution Sandbox + Transaction Engine v1.0

This is the first controlled mutation layer.

## Allowed
Only:
- CreateTextFile
- ReplaceTextFile
- paths inside sandbox
- explicit approval for non-dry-run

## Required
- dry-run
- precondition checks
- approval token
- audit log
- snapshots
- rollback on failure

## Blocked
- arbitrary process execution
- executable/script payloads
- path traversal
- mutation without explicit approval
- autonomous authority

## Critical architecture rule

This core executor is NOT allowed to mutate a real Unity project yet.
A future Unity executor must expose narrowly scoped operations through the same
transaction interface, with Unity-specific rollback/snapshot semantics.
