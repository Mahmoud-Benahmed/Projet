// API/Controllers/PaymentsController.cs
using ERP.PaymentService.Application.DTO;
using ERP.PaymentService.Application.Interfaces;
using ERP.PaymentService.Domain;
using ERP.PaymentService.Properties;
using Microsoft.AspNetCore.Mvc;

namespace ERP.PaymentService.API.Controllers;

[ApiController]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IPaymentService paymentService,
        ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    // GET payment/{id}
    [HttpGet(ApiRoutes.Payments.GetById)]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var payment = await _paymentService.GetByIdAsync(id);
        return payment is null ? NotFound() : Ok(payment);
    }

    // GET payment/number/{number}
    [HttpGet(ApiRoutes.Payments.GetByNumber)]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByNumber([FromRoute] string number)
    {
        var payment = await _paymentService.GetByNumberAsync(number);
        return payment is null ? NotFound() : Ok(payment);
    }

    // GET payment/client/{clientId}?pageNumber=1&pageSize=10
    [HttpGet(ApiRoutes.Payments.GetByClientId)]
    [ProducesResponseType(typeof(PagedResultDto<PaymentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByClientId(
        [FromRoute] Guid clientId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _paymentService.GetByClientIdAsync(clientId, pageNumber, pageSize);
        return Ok(result);
    }

    // GET payment?pageNumber=1&pageSize=10&search=PAY
    [HttpGet(ApiRoutes.Payments.GetPaged)]
    [ProducesResponseType(typeof(PagedResultDto<PaymentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] PaymentStatus status= PaymentStatus.DONE,
        [FromQuery] string? search = null)
    {
        var result = await _paymentService.GetPagedAsync(pageNumber, pageSize, status, search);
        return Ok(result);
    }

    // GET payment/invoice/{invoiceId}
    [HttpGet(ApiRoutes.Payments.GetByInvoiceId)]
    [ProducesResponseType(typeof(List<PaymentSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByInvoiceId([FromRoute] Guid invoiceId)
    {
        var result = await _paymentService.GetSummaryByInvoiceIdAsync(invoiceId);

        if (!result.Any())
            return NotFound($"No payments found for invoice {invoiceId}.");

        return Ok(result);
    }

    // POST /payment
    [HttpPost(ApiRoutes.Payments.Create)]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreatePaymentDto dto)
    {
        var payment = await _paymentService.CreateAsync(dto);
        return CreatedAtAction(
            actionName: nameof(GetById),
            routeValues: new { id = payment.Id },
            value: payment);
    }

    // PUT /payment/{id}/details
    [HttpPut(ApiRoutes.Payments.CorrectDetails)]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CorrectDetails(
        [FromRoute] Guid id,
        [FromBody] CorrectPaymentDto dto)
    {
        var payment = await _paymentService.CorrectDetailsAsync(id, dto);
        return Ok(payment);
    }

    // PATCH /payment/{id}/cancel
    [HttpPatch(ApiRoutes.Payments.Cancel)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Cancel([FromRoute] Guid id)
    {
        await _paymentService.CancelAsync(id);
        return NoContent();
    }


    // GET payment/stats
    [HttpGet(ApiRoutes.Payments.GetStats)]
    [ProducesResponseType(typeof(List<PaymentStatsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStats()
    {
        var result = await _paymentService.GetStatsAsync();

        return Ok(result);
    }
}