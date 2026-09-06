# UPA 24K Gold Product Roadmap

This roadmap reconciles the approved frozen baseline v0.1 with the product-vision MIS.
It is evidence-gated: a phase is complete only when its proof artifact exists.

## Product north star

UPA attaches to a consuming project, understands its project/hierarchy/inspector
knowledge, maintains authoritative records, detects change or missing items, assists
with safe deterministic fixes, and produces auditable trust evidence. Unity remains a
reference adapter, not UPA's identity.

## Current baseline

- **Implemented:** platform-neutral workflow contract, approval, preview, rollback,
  output verification, adapter registry, Unity adapter, REST/MCP/.NET trust interfaces,
  CI evidence and protected `master`.
- **Proven:** release build and GitHub quality gate; API concurrency hardening;
  filesystem workflow tests.
- **Not yet proven:** persistent project asset registry, continuous reconciliation,
  native project menu/panel, real consumer attachment, four historical tick-box labels,
  large/stress performance, and multiple non-Unity host adapters.

## Delivery phases

### P0 — Knowledge and safety proof

1. Adversarial C# parser and script-to-inspector evidence.
2. Project → hierarchy → inspector → asset relationship model.
3. Explicit `CONFIRMED / INFERRED / UNKNOWN / MISSING / CONFLICTED / STALE` facts.
4. Cache, cancellation, rollback boundaries and stale/conflict tests.

**Exit:** real-project fixture scan, targeted tests, regression suite, and before/after
re-scan evidence all pass.

### P1 — Project attachment and registry

1. Versioned `ProjectAttachmentManifest` with owner, adapter, permissions and hashes.
2. Persistent item records keyed by native host identity.
3. Rename/move/reload/change reconciliation with stale-record detection.
4. Safe uninstall/removal of UPA-owned integration.

**Exit:** fresh consumer project proves install → restore → scan → registry → rescan;
native identity remains authoritative.

### P2 — Action center and user surface

1. SCAN → GROUP → COMPLETE ACTION LIST → SELECT OWNER → BATCH APPLY → VERIFY.
2. AUTO / ASSIST / HUMAN / UNKNOWN decision classes.
3. Project-native menu/panel as a separate UI layer.
4. Four tick-box labels only after historical evidence recovery; never invent labels.

**Exit:** one user session resolves deterministic findings in a batch and shows
actionable human-owned findings without exposing internal trust details.

### P3 — Host adapters and compatibility

1. Adapter SDK with capability/version/permission declarations and isolation.
2. Unity reference adapter hardening.
3. Filesystem adapter productionization.
4. Web, Python and desktop adapters only after real-host fixtures exist.
5. VERIFIED / PARTIAL / UNSUPPORTED / UNKNOWN compatibility matrix.

**Exit:** each claimed adapter has a real-host proof; unsupported hosts fail closed.

### P4 — Performance and operations

1. Incremental scan, cache, lazy loading, bounded parallelism and resume.
2. Small/medium/large/stress profiles with time, memory and cancellation thresholds.
3. CI PR, main, release and nightly gates with timeout = failure.
4. Developer-time measurement: discovery, onboarding, rework and context-switch time.

**Exit:** benchmark JSON and trend comparison are retained as CI artifacts; no silent
test exclusion.

### P5 — Trust, release and provenance

1. Audit answers Who/What/When/Why/Before/After/Evidence/Approval/Plan/Result/
   Verification/Rollback.
2. Security threat model, dependency scan, malformed-project tests and adapter isolation.
3. Fresh consumer attachment proof: SDK install → restore → pipeline → AuditTrail →
   Trust Emission → Trust Anchor.
4. SHA-256 manifest, signed release evidence, Git tag and release audit.

**Exit:** release checklist, reproducible evidence bundle and independent review pass.

## Governance rules

- No real evidence, no core change.
- No core change without regression proof.
- No feature complete until real-project verification passes.
- Unknown evidence is never guessed.
- Every phase reports implemented, proven, not-proven and blocked items separately.
