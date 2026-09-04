using System;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using UPA.TrustLayer.Api.Contracts;
using UPA.TrustLayer.Api.Services;

namespace UPA.TrustLayer.Mcp.Tools;

public class InspectTrustTool
{
    private readonly ITrustInspectionAdapter _adapter;

    public InspectTrustTool(ITrustInspectionAdapter adapter)
    {
        _adapter = adapter;
    }

    [McpServerTool(Name = "inspect_trust")]
    [System.ComponentModel.Description("Looks up a previously emitted certificate chain by its entry ID.")]
    public async Task<TrustInspectResponse> ExecuteAsync(
        string id,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("id is required", nameof(id));

        try
        {
            var response = await _adapter.InspectAsync(id, cancellationToken);
            return response;
        }
        catch (TrustInspectionNotFoundException ex)
        {
            throw new InvalidOperationException($"[TRUST_NOT_FOUND] {ex.Message}", ex);
        }
    }
}
