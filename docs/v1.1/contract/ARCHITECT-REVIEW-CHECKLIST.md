# UPA Trust Contract v1.1 — Architect Review Checklist

Status: DRAFT

## A. Source Mapping

- [x] TrustEmissionRequest mapping verified
- [x] RunId mapping verified
- [x] AuditTrail mapping verified
- [x] CertificateChainEntry mapping verified
- [x] Required/optional status verified from source

## B. JSON Contract

- [x] Language-neutral
- [x] Draft-07 compliant
- [x] No non-standard schema keywords
- [x] Nullability represented correctly
- [x] Additional properties policy reviewed

## C. Semantics

- [x] No V1.0 semantic changes
- [x] Opaque AuditTrail decision reviewed
- [x] Hash/fingerprint representations reviewed
- [x] Date/time representation reviewed

## D. Error Model

- [x] Idempotency conflict mapping reviewed
- [x] Bundle collision mapping reviewed
- [x] HTTP status mappings remain proposed until approved

## E. Versioning

- [x] External v1 version boundary
- [x] Core V1.0 version independent
- [x] Breaking-change policy defined

## F. Examples

- [x] Valid request
- [x] Successful response
- [x] Failure response
- [x] Example values clearly marked as examples

## G. Implementation Gate

REST implementation: BLOCKED until approval.
MCP implementation: BLOCKED until approval.
Contract freeze: BLOCKED until approval.
