# UPA Product Rules and Footprint Contract

This document prevents UPA from becoming an opaque, invasive or maintenance-heavy
tool. Every adapter and feature must obey these rules.

## Product footprint

UPA must declare and measure four footprints:

- **Data:** files read/written, records retained, retention period and redaction.
- **Runtime:** CPU, memory, disk, network and scan duration.
- **Permission:** paths, processes, APIs and host capabilities used.
- **Human:** approvals requested, decisions deferred and developer time saved.

No feature is complete without a footprint report for a representative small and large
project.

## Non-negotiable rules

1. UPA never silently mutates a host project.
2. Native host identity remains authoritative; UPA identities are references only.
3. Unknown evidence is never converted into a guess.
4. Every mutation follows preview → approval → apply → verify → rollback/audit.
5. Every adapter declares capabilities, versions, permissions and unsupported cases.
6. UPA-owned files are manifest-tracked, hashed and safely removable.
7. No network access is required for local analysis unless explicitly enabled.
8. Secrets and source content are not uploaded by default.
9. Internal trust/certificate details stay behind the user-facing surface.
10. A timeout, skipped test or missing evidence is a release failure.
11. Generated outputs belong in CI artifacts, not source history.
12. Existing tools are integrated where they are authoritative; UPA does not duplicate
    mature scanners without a measurable advantage.

## Unique product pillars

### Project Knowledge Graph

Versioned relationships connect project, hierarchy, inspector, assets, dependencies,
owners and evidence. Every edge has confidence and freshness.

### Evidence Time Machine

Each reconciliation records before state, decision, approval, change, after state,
verification and rollback point. Historical state must be explainable, not merely logged.

### Developer-Time ROI

UPA measures discovery time, onboarding time, rework avoided, false positives, fix
success rate and human decisions deferred. A feature that increases total developer
time must not be called an improvement.

## Rule ownership

Rules are layered and traceable:

`UPA defaults → studio policy → project policy → temporary exception`

Every suppression has an owner, reason, expiry and audit entry. Temporary exceptions
must not silently become permanent behavior.

## Acceptance gate

A feature is accepted only when its problem, alternatives, footprint, permissions,
failure behavior, rollback, regression tests, real-project proof and developer-time
impact are documented.
