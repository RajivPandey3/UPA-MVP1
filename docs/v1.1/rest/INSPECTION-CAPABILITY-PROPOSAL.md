
# V1.1 Trust Inspection Capability Proposal

## Status

APPROVED — CAPABILITY DECISION

This document records the approved V1.1 inspection capability. It does not authorize changes to the frozen V1.1 core contract and does not modify V1.0 trust-emission semantics.

## Context

The V1.1 REST surface exposes the `inspect_trust` operation. The approved capability now allows inspection of an emitted trust record by `entry_id`, backed by the existing V1.0 durable trust state.

## Approved Capability

The capability:

1. Reads existing V1.0 trust state.
2. Locates a persisted trust record by `entry_id`.
3. Returns the persisted `CertificateChainEntry`.
4. Uses `status = "emitted"` for an existing record.
5. Returns HTTP 404 with `TRUST_NOT_FOUND` when no matching record exists.
6. Does not introduce a second trust-state store.
7. Does not modify V1.0 emission behavior.
8. Does not introduce a new verification algorithm.

## Compatibility

The existing REST operation remains:

`GET /v1/trust/{id}`

The existing response shape remains:

* `id`
* `status`
* `certificate_chain`

No breaking change to the frozen V1.1 core contract is introduced.

## Governing Constraints

The V1.0 trust core must not be rewritten or duplicated.

The frozen V1.1 contract remains authoritative for approved external contract behavior.

Any breaking external `/v1` contract change requires a new major external API version.

Backward-compatible additions remain subject to contract review and approval.
