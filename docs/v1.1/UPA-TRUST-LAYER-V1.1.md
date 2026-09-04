# UPA Trust Layer v1.1

## Status

IN PROGRESS

## Baseline

V1.0 baseline commit: d685ad8

## Phase 1

Trust Contract definition.

Status: APPROVED - CONTRACT FROZEN.

## Rule

The V1.0 trust core must not be rewritten or duplicated.

## Planned Interfaces

- REST API
- MCP Tools
- Native SDKs

## Initial Operations

- emit_trust
- verify_trust
- inspect_trust

## Phase 2 - REST API

Status: IMPLEMENTED - INSPECTION DEFERRED.

### emit_trust

Implemented and wired to the frozen V1.0 TrustEmitter.

### verify_trust

Implemented through the V1.1 trust verification service and adapter, backed by V1.0 certificate-chain verification.

### inspect_trust

Deferred. The current V1.0 TrustEmitter has no public lookup/inspection capability. The frozen REST mapping requires inspection to be backed by an existing V1.0 capability, so no new V1.0 persistence or lookup semantics are introduced.

## Next Step

Define and implement the next approved V1.1 interface or capability without modifying or duplicating the V1.0 trust core.
