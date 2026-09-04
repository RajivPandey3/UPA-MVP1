# V1.1 Performance Limitations

STATUS: APPROVED - DOCUMENTATION

## Overview
The V1.1 Trust Layer implementation relies on the frozen V1.0 Core logic (`TrustEmitter`), which persists state synchronously via file system I/O (JSON serialization with an atomic `.tmp` swap).

## Measured Baseline Throughput
Bounded benchmarking indicates the following approximate local throughput limitations:
*   **New Emissions:** ~90 requests/sec.
*   **Idempotent Emissions (Cache Hits):** ~645 requests/sec.

*Note: These measurements were taken on local benchmark hardware. They are provided as bounded evidence and do NOT constitute a production SLA.*

## Production Considerations
*   **Concurrency:** The `TrustEmitter` enforces strict serialization via an in-memory lock (`lock (_lock)`). Simultaneous API requests are safely serialized, but this bottleneck prevents horizontal scaling of a single trust ledger across multiple pods/instances.
*   **Storage:** Trust state is stored in a single JSON file. As the ledger grows, the deserialization and serialization cost per request will increase linearly.
*   **Conclusion:** The current architecture passes MVP1 constraints but is not designed for unlimited high-throughput SLA capacity without a future database-driven architectural redesign.
