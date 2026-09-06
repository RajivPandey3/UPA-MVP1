# Adapter Compatibility Matrix

| Adapter/fixture | Proof | Footprint evidence | Status |
|---|---|---|---|
| .NET real project | Build, scan, mutation, restore, cancellation, resume | Build time and bytes | VERIFIED |
| Python real project | Execute, mutation, restore | Execution time and bytes | VERIFIED |
| Static Web project | Asset validation, mutation, restore | Validation time and bytes | VERIFIED |
| Unity 6000.0.36f1 | Editor found; batch proof attempted | License/token failure retained | BLOCKED |

Unity is blocked by the local Personal-license token error
(`TimeStamp validation failed` / `No valid Unity Editor license found`), not by a
missing executable or unsupported project. No Unity capability is marked verified
until a real Editor run produces retained evidence.
