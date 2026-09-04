# V1.1 Operational Hand-Off & Developer Guide

This document provides external consumers and operators with the practical knowledge required to build, test, run, and consume the UPA V1.1 Trust Layer.

## 1. Building and Testing
The repository utilizes a standard .NET 8 structure.
*   **Release Build:** `dotnet build .\UPA-MVP1.sln -c Release`
*   **Full Test Suite:** `dotnet test .\UPA-MVP1.sln -c Release --no-build`

## 2. API Configuration and Startup
The Trust Layer REST API is located in `src\UPA.TrustLayer.Api`.
*   **Startup:** `dotnet run -c Release --project src\UPA.TrustLayer.Api`
*   **Configuration:** The API strictly requires the `TrustEmission:StateFilePath` configuration setting. This dictates where the durable ledger JSON file is saved.
*   **Environment Variable Example:** `TrustEmission__StateFilePath=/path/to/durable/state.json`

## 3. Storage and Backups
*   **Format:** The durable trust state is a single JSON file.
*   **Permissions:** The process running the API or MCP server requires `Write` and `Modify` permissions on the target directory to support the atomic `.tmp` file replacement.
*   **Backups:** Since operations are append-only, standard file backups (e.g., cron-based copying) are sufficient. Temporary `.tmp` files are used during writes to ensure crash consistency.

## 4. MCP Interoperability (stdio)
The Model Context Protocol (MCP) server is available at `src\UPA.TrustLayer.Mcp`.
*   It operates over standard input/output (`stdio`).
*   **Execution:** `dotnet run -c Release --project src\UPA.TrustLayer.Mcp`
*   **Prerequisite:** Ensure standard logging is not piped to `stdout` to avoid corrupting the JSON-RPC streams. The application uses `LogLevel.Error` by default on the console. 
*   **Environment Variable:** You must set `TrustEmission__StateFilePath`.

## 5. Native SDK Consumption
The C# Thin Client is available at `src\UPA.TrustLayer.Client`.
*   **Package Name:** `UPA.TrustLayer.Client` (Target: `net8.0`)
*   **Usage:**
    ```csharp
    var httpClient = new HttpClient { BaseAddress = new Uri("http://api-url") };
    ITrustLayerClient client = new TrustLayerClient(httpClient);
    var certs = await client.EmitTrustAsync("run_1", "bundle_1", "hash_1", "snapshot");
    ```
*   **Error Handling:** Expect `HttpRequestException` for network failures, and domain exceptions (e.g., `TrustErrorResponse`) for HTTP 400/409 conflicts (such as `BUNDLE_COLLISION`).

## 6. Known Limitations
1.  **Contract Documentation:** There is a pre-existing discrepancy regarding `certificate_chain` on `TrustEmitRequest` in the frozen JSON schema. The runtime implementation (API/SDK) is authoritative: `certificate_chain` MUST be provided (can be an empty array).
2.  **Performance:** The V1.0 Core enforces an in-memory lock for synchronous file I/O. Throughput is bounded at ~90 emits/s locally. It is not designed for horizontally scaled deployments spanning multiple instances against a single file.
