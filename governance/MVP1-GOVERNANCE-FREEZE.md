# UPA MVP-1 Governance Freeze

Effective for RC1:

1. Auto-approval is forbidden.
2. Preview bypass is forbidden.
3. Validation bypass is forbidden.
4. A completed run does not create standing execution authority.
5. Unknown operations are rejected.
6. Ambiguous targets are rejected.
7. Required parameters must validate before binding.
8. Real mutations must pass through the governed executor.
9. Transaction failure must trigger rollback policy.
10. Audit records are part of the completion contract.

Changes to these rules require a new milestone/version.
