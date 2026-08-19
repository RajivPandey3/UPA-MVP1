# Milestone Integration Gate
Separate milestone PASS results do not by themselves prove integration.

## 1. Integration Declaration

Every new MVP must declare:

- previous milestones it depends on;
- whether each dependency is required, optional, or absent;
- required integration level:
  - contract/API;
  - build-time;
  - test-time;
  - runtime;
  - governance boundary.

An undeclared dependency must not be inferred as a required integration.

## 2. Dependency Check

For each declared dependency:

- inspect project references;
- inspect contracts/interfaces;
- inspect documented architecture;
- identify the actual dependency boundary.

If no dependency exists and the milestone is explicitly independent,
the relationship may be classified as `INDEPENDENT-BY-DESIGN`.

## 3. Compatibility Check

When integration is required, verify the applicable level:

- Contract/API compatibility
- Build compatibility
- Test compatibility
- Runtime compatibility, when required
- Governance-boundary compatibility

Only applicable checks are required.

## 4. Evidence Rule

An integration claim requires evidence appropriate to the declared
integration level.

Examples:

- contract integration → compatible contract evidence;
- build integration → combined build evidence;
- test integration → integration-test evidence;
- runtime integration → controlled runtime evidence.

Separate milestone PASS results do not by themselves prove integration.

## 5. Governance Boundary

Integration must not silently introduce:

- execution authority;
- approval bypass;
- validation bypass;
- preview bypass;
- unauthorized state mutation.

A component that is intentionally evidence-only must remain evidence-only.

## 6. Verdicts

### PASS

Required integration exists and all applicable checks have supporting
evidence.

### INDEPENDENT-BY-DESIGN

No integration is required by the declared architecture, and the milestone
is intentionally independent.

### REQUIRED-BUT-NOT-VERIFIED

Integration is required, but sufficient evidence has not yet been produced.

### FAIL

Required integration exists but one or more applicable checks fail.

## 7. Evidence Record

For every milestone, record:

- milestone identifier/version;
- dependency declarations;
- required integration level;
- checks performed;
- evidence location;
- final verdict;
- unresolved limitations.

## 8. Future Milestones

MVP-3 and later milestones must use this gate before an integration claim is
made.

The gate must not require a full combined build/test when the declared
architecture establishes that the milestones are independent.

## 9. Rule

Never convert `REQUIRED-BUT-NOT-VERIFIED` to `PASS` merely because the
individual milestones pass independently.
