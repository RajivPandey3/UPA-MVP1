
# V1.1 Trust Inspection Capability Decision

## Status

APPROVED — CAPABILITY DECISION

This document approves the V1.1 inspection capability described below. It does not authorize changes to the frozen V1.1 core contract and does not modify V1.0 trust-emission semantics.

## Approved Decisions

### 1. Lookup Capability

Expose a read-only V1.1-facing lookup capability backed by the existing V1.0 durable trust state.

The capability may locate an emitted trust record and return the persisted `CertificateChainEntry` required by the existing V1.1 inspection response.

No second trust-state store may be introduced.

### 2. Lookup Identifier

The approved lookup identifier is `entry_id`.

This identifier is already exposed by the existing V1.1 trust-emission response and corresponds to the persisted V1.0 `CertificateChainEntry`.

`run_id` remains the V1.0 idempotency key and `artifact_bundle_id` remains the V1.0 bundle identity/collision key. They are not the approved inspection lookup identifier.

### 3. Status

For an existing trust record, the inspection response uses:

`status = "emitted"`

No additional lifecycle or verification status values are introduced by this capability.

The inspection operation reports the existence of the emitted trust record; it does not perform a new verification operation.

### 4. Certificate Chain

The `certificate_chain` returned by inspection is sourced from the persisted V1.0 `CertificateChainEntry` associated with the located trust record.

The implementation must not reconstruct, replace, or independently generate certificate-chain data.

### 5. Not Found

When the requested `entry_id` does not correspond to an existing persisted trust record, the REST operation returns:

HTTP `404 Not Found`

with a stable error code:

`TRUST_NOT_FOUND`

The response message may describe that the requested trust entry was not found, but the stable machine-readable code is `TRUST_NOT_FOUND`.

### 6. Capability Placement

The lookup capability belongs in a V1.1-facing abstraction backed by existing V1.0 durable state.

The V1.0 trust-emission core must not be rewritten, duplicated, or given new emission semantics.

If an internal V1.0-backed read access mechanism is required, it must preserve the existing persisted state format and semantics.

### 7. Compatibility and Versioning

The existing V1.1 REST operation remains:

`GET /v1/trust/{id}`

The existing inspection response shape remains:

* `id`
* `status`
* `certificate_chain`

No breaking change is authorized.

Any future breaking change to the external `/v1` contract requires a new major external API version.

## Implementation Constraints

Implementation must:

1. Use `entry_id` as the lookup identifier.
2. Read existing V1.0 durable trust state.
3. Avoid a second persistence store.
4. Avoid modifying V1.0 emission behavior.
5. Avoid duplicating certificate-chain verification.
6. Return the persisted certificate-chain entry.
7. Return `status = "emitted"` for an existing record.
8. Return HTTP 404 with `TRUST_NOT_FOUND` when no record exists.
9. Keep transport-specific mapping in the V1.1 REST layer.
10. Preserve the frozen TrustEmissionRequest and CertificateChainEntry contracts.

## Explicit Non-Goals

This decision does not authorize:

* changes to the frozen TrustEmissionRequest contract;
* changes to CertificateChainEntry;
* new trust persistence semantics;
* independent inspection storage;
* a new verification algorithm;
* modification of V1.0 emission behavior;
* additional status values;
* changes to `/v1/trust/emit`;
* MCP or SDK implementation.

## Implementation Gate

The inspection capability is now approved for implementation subject to the constraints above.

The next implementation step is to design the V1.1-facing read abstraction and its V1.0-backed data access without modifying the V1.0 emission core.
