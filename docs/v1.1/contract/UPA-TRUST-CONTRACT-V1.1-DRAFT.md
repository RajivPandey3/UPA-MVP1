# UPA Trust Contract v1.1

## Status
APPROVED — CONTRACT FROZEN

## Source of Truth
This contract must be derived from the actual V1.0 implementation
and existing V1.0 tests.

V1.0 baseline: d685ad8

## Required Mappings
- TrustEmissionRequest
- RunId
- AuditTrail
- CertificateChainEntry

## Rules
1. Do not invent V1.0 fields.
2. Do not change V1.0 semantics.
3. Clearly mark any V1.1 proposed capability.
4. Keep the contract language-neutral.
5. REST and MCP must consume this contract after approval.

## Proposed JSON Schema

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "UPA Trust Contract",
  "definitions": {
    "TrustEmissionRequest": {
      "type": "object",
      "properties": {
        "run_id": { "type": "string", "description": "Unique identifier for the execution run." },
        "artifact_bundle_id": { "type": "string" },
        "artifact_hash": { "type": "string" },
        "finalized_audit_snapshot": { "type": "string", "description": "The serialized MVP-1 AuditTrail." }
      },
      "required": ["run_id", "artifact_bundle_id", "artifact_hash", "finalized_audit_snapshot"]
    },
    "CertificateChainEntry": {
      "type": "object",
      "properties": {
        "entry_id": { "type": "string" },
        "bundle_id": { "type": "string" },
        "bundle_fingerprint": { "type": "string" },
        "sequence": { "type": "integer" },
        "registry_certificate_id": { "type": "string" },
        "registry_certificate_hash": { "type": "string" },
        "registry_certificate_fingerprint": { "type": "string" },
        "previous_registry_certificate_id": { "type": ["string", "null"] },
        "previous_registry_certificate_hash": { "type": ["string", "null"] },
        "certified_utc": { "type": "string", "format": "date-time" }
      },
      "required": [
        "entry_id", "bundle_id", "bundle_fingerprint", "sequence",
        "registry_certificate_id", "registry_certificate_hash",
        "registry_certificate_fingerprint", "certified_utc"
      ]
    },
    "TrustError": {
      "type": "object",
      "properties": {
        "code": { "type": "string" },
        "message": { "type": "string" }
      },
      "required": ["code", "message"]
    }
  }
}
`

## Request Example (emit_trust)

```json
{
  "run_id": "run-a1b2",
  "artifact_bundle_id": "bundle-v1",
  "artifact_hash": "sha256:abc...",
  "finalized_audit_snapshot": "{\"events\":[]}"
}
`

## Success Response Example

```json
{
  "entry_id": "entry-987",
  "bundle_id": "bundle-v1",
  "bundle_fingerprint": "sha256:def...",
  "sequence": 1,
  "registry_certificate_id": "cert-123",
  "registry_certificate_hash": "sha256:456...",
  "registry_certificate_fingerprint": "sha256:789...",
  "previous_registry_certificate_id": null,
  "previous_registry_certificate_hash": null,
  "certified_utc": "2026-09-03T10:00:00Z"
}
`

## Failure Response Example

```json
{
  "code": "IDEMPOTENCY_CONFLICT",
  "message": "Conflicting payload for existing RunId"
}
```


## Approved Error Response Model

* Emission conflicts use stable language-neutral codes: IDEMPOTENCY_CONFLICT and BUNDLE_COLLISION.
* These codes map to the V1.0 IdempotencyConflictException and BundleCollisionException behaviors.
* REST emission conflicts map to HTTP 409 Conflict.
* The REST error response is a direct object containing required code and message, with optional transport-level request_id.
* Verification responses use valid and errors; verification errors remain strings matching the current API boundary.
## Additional Properties Policy
- TrustEmissionRequest and CertificateChainEntry use additionalProperties: false.
- Unknown properties are rejected for these core contract objects.

## Versioning Proposal
- REST endpoints will use /v1/trust/...
- V1.1 capabilities (like status checks) will not modify core trust fields. Any new fields specific to the transport layer (REST/MCP) will be kept outside the core TrustEmissionRequest/CertificateChainEntry objects, possibly wrapping them in envelopes if needed, or preserving direct mapping for simplicity.

## Breaking-Change Policy
- Breaking changes to the external /v1 contract require a new major external API version.
- Backward-compatible additive changes may remain within the existing external version, subject to contract review and approval.
- External API versioning remains independent from the internal V1.0 core implementation.

## Approved Decisions
1. finalized_audit_snapshot remains an opaque string for exact hash parity with the V1.0 .NET contract.
2. V1.0 IdempotencyConflictException maps to IDEMPOTENCY_CONFLICT and HTTP 409 Conflict.
3. V1.0 BundleCollisionException maps to BUNDLE_COLLISION and HTTP 409 Conflict.
