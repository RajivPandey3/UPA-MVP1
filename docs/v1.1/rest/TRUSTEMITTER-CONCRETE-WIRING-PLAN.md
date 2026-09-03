# UPA V1.1 — TrustEmitter Concrete Wiring Plan

Status: DISCOVERY COMPLETE / IMPLEMENTATION PENDING

## Source of truth

The V1.0 implementation remains authoritative:

D:\UPA-MVP1\verified9\src\UPA.MVP3.TrustEmission\TrustEmitter.cs

## Rules

1. The V1.0 TrustEmitter implementation MUST NOT be modified.
2. Constructor dependencies MUST come directly from V1.0 source evidence.
3. Existing V1.0 test construction patterns take precedence over guesses.
4. REST transport concerns remain outside TrustEmissionRequest.
5. API authentication, authorization and rate limiting remain infrastructure concerns.
6. V1.0 exceptions remain unchanged.
7. HTTP 409 mapping belongs only to the REST adapter.

## Required next implementation

The concrete adapter must:

- construct or receive TrustEmitter using the exact V1.0 constructor;
- map TrustEmitRequest -> TrustEmissionRequest exactly;
- preserve finalized_audit_snapshot as an opaque string;
- preserve certificate-chain field semantics;
- invoke the existing V1.0 EmitAsync operation;
- return the existing V1.0 result through the API response contract;
- map IdempotencyConflictException -> HTTP 409;
- map BundleCollisionException -> HTTP 409;
- avoid changing V1.0 behavior.

## Dependency evidence

See:
- V1.0-TRUSTEMITTER-DEPENDENCY-EVIDENCE.txt
- V1.0-TRUSTEMITTER-TEST-CONSTRUCTION-EVIDENCE.txt (if present)

## Implementation gate

Concrete DI registration is NOT approved until the exact constructor
signature and every required dependency have been identified from
source/test evidence.
