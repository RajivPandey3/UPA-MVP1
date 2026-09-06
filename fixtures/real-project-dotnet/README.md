# Real .NET Project Proof Fixture

This is the smallest buildable external project used for real-project verification.

## Proof protocol

1. Restore and build this project successfully.
2. Scan the project directory and record project, source, and settings evidence.
3. Project the scan into the UPA knowledge graph.
4. Change `Program.cs`, rescan, and require one `Changed` reconciliation result.
5. Restore the original file, rescan, and require an `Unchanged` result.
6. Retain the scan, reconciliation, timing, and footprint evidence for the exact fixture hash.

The fixture is intentionally outside `src/` and is not a product implementation.
