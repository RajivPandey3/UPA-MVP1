# UPA Trust Contract v1.1 — Draft Review Rules

## Status

DRAFT — NOT APPROVED

## Authority

The actual V1.0 implementation and V1.0 tests are authoritative.

Baseline:
d685ad8

## Required Review

For every proposed contract field:

- identify exact V1.0 source type
- identify exact source property
- identify JSON representation
- identify required/optional status
- identify evidence
- do not invent semantics

## Audit Snapshot

inalized_audit_snapshot must remain an opaque string in the
contract unless the V1.0 implementation provides evidence that
it can safely be represented as structured JSON without changing
cryptographic/byte-level semantics.

Current status:
PROPOSED — preserve opaque string representation.

## Error Mapping

The following mappings are proposals only:

- IdempotencyConflictException -> HTTP 409
- BundleCollisionException -> HTTP 409

Do not freeze these mappings until the contract review is complete.

## JSON Schema

The schema must use valid JSON Schema Draft-07 syntax.

Do not use non-standard Draft-07 keywords such as:

    nullable: true

For nullable strings, use the standard Draft-07 form:

    "type": ["string", "null"]

## Versioning

Keep external API versioning independent from the internal V1.0
core implementation.

Initial external API version:

v1

## Prohibited

Do not:

- modify V1.0 trust semantics
- add speculative V1.1 core fields
- implement REST
- implement MCP
- create EXE
- create arbitrary package formats
