# One-command verification

From the release-candidate directory:

```text
python verification/verify.py
```

Exit code:
- `0` = release contract passes
- `1` = release contract fails

The generated `verification-report.json` is machine-readable and suitable for CI.
