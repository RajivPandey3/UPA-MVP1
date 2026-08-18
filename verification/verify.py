#!/usr/bin/env python3
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

REQUIRED = [
    "ProjectModel",
    "Health",
    "IntentGrammar",
    "OperationRegistry",
    "PlanCompiler",
    "Validator",
    "Preview",
    "Approval",
    "PlanToExecutorAdapter",
    "TargetResolver",
    "UnityExecutor",
    "TransactionUndo",
    "Audit",
    "VerificationHarness",
]

def main():
    manifest = json.loads(
        (ROOT / "release-manifest.json").read_text(encoding="utf-8")
    )

    failures = []

    if manifest["release"] != "1.8.0-rc1":
        failures.append("Release version mismatch.")

    governance = manifest["governance"]
    for key in (
        "auto_approval",
        "preview_bypass",
        "validation_bypass",
        "standing_execution_authority",
    ):
        if governance.get(key) is not False:
            failures.append(f"Governance violation: {key} must be false.")

    components = set(manifest["required_components"])
    for component in REQUIRED:
        if component not in components:
            failures.append(f"Missing required component: {component}")

    policy = manifest["verification_policy"]
    if policy["green_requires_zero_failures"] is not True:
        failures.append("Green-build policy is invalid.")
    if policy["blocked_cases_are_not_passes"] is not True:
        failures.append("Blocked-case policy is invalid.")
    if policy["skipped_cases_are_not_passes"] is not True:
        failures.append("Skipped-case policy is invalid.")

    report = {
        "release": manifest["release"],
        "status": "PASS" if not failures else "FAIL",
        "failures": failures,
        "checks": {
            "manifest": not bool(failures),
            "governance": all(v is False for v in governance.values()),
            "component_inventory": all(x in components for x in REQUIRED),
            "verification_policy": all(policy.values()),
        },
    }

    out = ROOT / "verification-report.json"
    out.write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(json.dumps(report, indent=2))
    return 0 if not failures else 1

if __name__ == "__main__":
    sys.exit(main())
