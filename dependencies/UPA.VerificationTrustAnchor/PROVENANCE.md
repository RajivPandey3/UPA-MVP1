# Bundled trust anchor

Source: `UPA-MVP2/artifacts/v20.0-final/src/UPA.VerificationTrustAnchor/TrustAnchor.cs` from the local dependency used by this repository before the portability repair.

The implementation is unchanged after normalizing line endings and the trailing newline. It is included once here so builds do not require a sibling directory on the original developer's laptop. `Directory.Build.targets` redirects the existing trust-anchor project references to this bundled project.

Bundled `TrustAnchor.cs` SHA-256: `B98565294085B95CACDF6FDF5A2078E8D7E1129CD6546607D360C720B14AC9E9` (hash of the file as written during verification; checkout line-ending conversion may change byte hashes).

Update this dependency deliberately from a reviewed upstream release; do not edit the cryptographic implementation as part of routine integration changes.
