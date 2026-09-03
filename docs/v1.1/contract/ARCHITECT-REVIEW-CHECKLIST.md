# UPA Trust Contract v1.1 — Architect Review Checklist

Status: DRAFT

## A. Source Mapping

- [ ] TrustEmissionRequest mapping verified
- [ ] RunId mapping verified
- [ ] AuditTrail mapping verified
- [ ] CertificateChainEntry mapping verified
- [ ] Required/optional status verified from source

## B. JSON Contract

- [ ] Language-neutral
- [ ] Draft-07 compliant
- [ ] No non-standard schema keywords
- [ ] Nullability represented correctly
- [ ] Additional properties policy reviewed

## C. Semantics

- [ ] No V1.0 semantic changes
- [ ] Opaque AuditTrail decision reviewed
- [ ] Hash/fingerprint representations reviewed
- [ ] Date/time representation reviewed

## D. Error Model

- [ ] Idempotency conflict mapping reviewed
- [ ] Bundle collision mapping reviewed
- [ ] HTTP status mappings remain proposed until approved

## E. Versioning

- [ ] External v1 version boundary
- [ ] Core V1.0 version independent
- [ ] Breaking-change policy defined

## F. Examples

- [ ] Valid request
- [ ] Successful response
- [ ] Failure response
- [ ] Example values clearly marked as examples

## G. Implementation Gate

REST implementation: BLOCKED until approval.
MCP implementation: BLOCKED until approval.
Contract freeze: BLOCKED until approval.
