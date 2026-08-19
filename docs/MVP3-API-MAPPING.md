# MVP-1 to MVP-2 API Semantic Mapping

This document establishes the exact field-by-field mapping required for MVP-3 to translate an MVP-1 execution into an MVP-2 Artifact Trust Chain attestation.

## 1. MVP-1 Execution Output (The Source)
When an MVP-1 pipeline successfully builds an artifact, it produces the following context:
- `RunId`: Identifies the execution pipeline instance.
- `AuditTrail`: The finalized audit payload text.
- `AuditHash`: `CanonicalHash.Sha256(AuditTrail.ToText())`.
- `ArtifactBundleId`: The ID of the built artifact (e.g., `release-v1`).
- `ArtifactHash`: The SHA-256 of the artifact bundle.

## 2. MVP-3 Evidence Sealing (The Bridge)
MVP-3 takes the execution output and seals it into a single trust evidence hash that MVP-2 can anchor:
- `BundleId` = `ArtifactBundleId`
- `BundleFingerprint` = `ArtifactHash`
- `RegistryFingerprint` = `CanonicalHash.Sha256(RunId + "\n" + AuditHash)`

*Note: The `RegistryFingerprint` acts as the cryptographic proof of the execution that built the artifact, safely decoupling the execution identity (`RunId`) from the artifact identity (`BundleId`).*

## 3. MVP-2 Factory Input (`RegistryCertificateInput`)
MVP-3 maps its sealed evidence into the MVP-2 factory parameters to generate the cryptographic certificate:
- `BundleId`: `BundleId`
- `BundleFingerprint`: `BundleFingerprint`
- `ChainRootCount`: `1` (For the initial builder attestation)
- `FirstChainRootCertificateId`: The ID of the generated Genesis `ChainRoot`
- `FirstChainRootFingerprint`: The fingerprint of the generated Genesis `ChainRoot`
- `LatestChainRootCertificateId`: Same as First
- `LatestChainRootFingerprint`: Same as First
- `OrderedChainRootCertificateIds`: `[FirstChainRootCertificateId]`
- `RegistryFingerprint`: The sealed execution evidence hash (`RegistryFingerprint`)

## 4. MVP-2 Registration (`CertificateChainEntry`)
Finally, MVP-3 constructs the `CertificateChainEntry` to be registered into the MVP-2 `RegistryCertificateChain`:
- `EntryId`: Unique GUID for this specific chain entry.
- `BundleId`: `BundleId`
- `BundleFingerprint`: `BundleFingerprint`
- `Sequence`: `1` (Since MVP-3 provides the initial builder attestation, it always starts the chain at 1. Subsequent attestations by downstream systems will use Sequence 2+)
- `RegistryCertificateId`: `RegistryCertificate.CertificateId`
- `RegistryCertificateHash`: `RegistryCertificate.CertificateHash`
- `RegistryCertificateFingerprint`: `RegistryCertificate.RegistryCertificateFingerprint`
- `PreviousRegistryCertificateId`: `null` (since Sequence 1)
- `PreviousRegistryCertificateHash`: `null` (since Sequence 1)
- `CertifiedUtc`: `RegistryCertificate.CertifiedUtc`

## Validation Conclusion
This mapping conclusively proves that the **Artifact Trust Model** satisfies all existing MVP-2 validation rules. The `RegistryFingerprint` natively accommodates the `RunId` + `AuditHash` execution evidence, preserving the `BundleFingerprint` strictly for the invariant Artifact Hash. This enables a clean, unmodified integration with MVP-2.
