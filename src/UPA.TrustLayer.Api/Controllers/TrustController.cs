using Microsoft.AspNetCore.Mvc;
using UPA.TrustLayer.Api.Contracts;
using UPA.TrustLayer.Api.Services;

namespace UPA.TrustLayer.Api.Controllers;

[ApiController]
[Route("v1/trust")]
public sealed class TrustController : ControllerBase
{
    private readonly ITrustEmissionAdapter _emitAdapter;
    private readonly ITrustVerificationAdapter _verifyAdapter;

    public TrustController(
        ITrustEmissionAdapter emitAdapter,
        ITrustVerificationAdapter verifyAdapter)
    {
        _emitAdapter = emitAdapter;
        _verifyAdapter = verifyAdapter;
    }

    [HttpPost("emit")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Emit(
        [FromBody] TrustEmitRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _emitAdapter.EmitAsync(
                request,
                cancellationToken);

            var mappedDto = new CertificateChainEntry
            {
                EntryId = result.EntryId,
                BundleId = result.BundleId,
                BundleFingerprint = result.BundleFingerprint,
                Sequence = result.Sequence,
                RegistryCertificateId = result.RegistryCertificateId,
                RegistryCertificateHash = result.RegistryCertificateHash,
                RegistryCertificateFingerprint = result.RegistryCertificateFingerprint,
                PreviousRegistryCertificateId = result.PreviousRegistryCertificateId,
                PreviousRegistryCertificateHash = result.PreviousRegistryCertificateHash,
                CertifiedUtc = result.CertifiedUtc
            };

            return Ok(mappedDto);
        }
        catch (Exception ex) when (
            ex.GetType().Name == "IdempotencyConflictException" ||
            ex.GetType().Name == "BundleCollisionException")
        {
            return Conflict(new TrustErrorResponse
            {
                Code = ex.GetType().Name,
                Message = ex.Message
            });
        }
    }

    [HttpPost("verify")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public async Task<IActionResult> Verify(
        [FromBody] TrustVerifyRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _verifyAdapter.VerifyAsync(
                request,
                cancellationToken);

            return Ok(result);
        }
        catch (NotImplementedException ex)
        {
            return StatusCode(
                StatusCodes.Status501NotImplemented,
                new TrustErrorResponse
                {
                    Code = "TRUST_VERIFY_NOT_IMPLEMENTED",
                    Message = ex.Message
                });
        }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public IActionResult Inspect(string id)
    {
        return StatusCode(
            StatusCodes.Status501NotImplemented,
            new TrustErrorResponse
            {
                Code = "TRUST_INSPECT_NOT_IMPLEMENTED",
                Message = "Inspection adapter is not implemented yet."
            });
    }
}
