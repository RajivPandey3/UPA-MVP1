# UPA V1.1 REST API — Phase 2B Mapping

Status: IMPLEMENTATION DRAFT

## Endpoint mapping

### POST /v1/trust/emit

External request:

- run_id
- artifact_bundle_id
- artifact_hash
- finalized_audit_snapshot
- certificate_chain

The adapter maps these fields to the existing V1.0
TrustEmissionRequest without changing V1.0 semantics.

### POST /v1/trust/verify

Verification behavior must be backed by an existing V1.0 capability.
No new verification semantics may be invented.

### GET /v1/trust/{id}

Inspection behavior must be backed by an existing V1.0 capability.
No new persistence/query semantics may be invented.

## Infrastructure boundary

The following are NOT TrustEmissionRequest business fields:

- API keys
- bearer tokens
- authentication metadata
- authorization metadata
- rate-limit metadata
- quotas
- transport tracing metadata

These belong to middleware/API infrastructure.

## Error mapping

Approved API-layer mappings:

- IdempotencyConflictException -> HTTP 409
- BundleCollisionException -> HTTP 409

The V1.0 exception classes themselves remain unchanged.

## Freeze rule

Do not modify the V1.0 TrustEmission implementation.
