// API/Controllers/RefundController.cs
using ERP.PaymentService.Application.DTO;
using ERP.PaymentService.Application.Interfaces;
using ERP.PaymentService.Domain;
using ERP.PaymentService.Properties;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace ERP.PaymentService.API.Controllers;

[ApiController]
public class RefundController : ControllerBase
{
    private readonly IRefundService _refundService;
    private readonly ILogger<RefundController> _logger;

    public RefundController(IRefundService refundService, ILogger<RefundController> logger)
    {
        _refundService = refundService;
        _logger = logger;
    }

    // GET /payment/refunds/stats
    [HttpGet(ApiRoutes.Refunds.GetStats)]
    [ProducesResponseType(typeof(RefundStatsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStats()
    {
        var stats= await _refundService.GetStatsAsync();
        return Ok(stats);
    }


    // GET /payment/refunds/{refundId}
    [HttpGet(ApiRoutes.Refunds.GetById)]
    [ProducesResponseType(typeof(RefundRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid refundId, CancellationToken ct)
    {
        var refund = await _refundService.GetByIdAsync(refundId, ct);
        return refund is null ? NotFound($"Refund '{refundId}' not found.") : Ok(refund);
    }

    // GET /payment/refunds/client/{clientId}
    [HttpGet(ApiRoutes.Refunds.GetByClientId)]
    [ProducesResponseType(typeof(RefundRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByClientId([FromRoute] Guid clientId,CancellationToken ct)
    {
        var refund = await _refundService.GetByClientIdAsync(clientId, ct);
        return Ok(refund);
    }

    // PATCH /payment/refunds/{refundId}/complete
    [HttpPatch(ApiRoutes.Refunds.Complete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Complete(
        [FromRoute] Guid refundId,
        [FromBody] CompleteRefundDto dto,
        CancellationToken ct)
    {
        await _refundService.CompleteRefundAsync(refundId, dto.ExternalReference, ct);

        _logger.LogInformation("Refund {RefundId} marked as complete.", refundId);

        return NoContent();
    }
}