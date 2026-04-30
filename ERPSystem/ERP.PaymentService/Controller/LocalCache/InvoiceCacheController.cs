using ERP.PaymentService.Application.DTO;
using ERP.PaymentService.Application.Interfaces.LocalCache;
using ERP.PaymentService.Properties;
using Microsoft.AspNetCore.Mvc;

namespace ERP.PaymentService.Controller.LocalCache;

[ApiController]
public class InvoiceCacheController : ControllerBase
{
    private readonly IInvoiceCacheService _invoiceCacheService;
    public InvoiceCacheController(IInvoiceCacheService invoiceCacheService)
    {
        _invoiceCacheService = invoiceCacheService;
    }

    [HttpGet(ApiRoutes.Invoices.GetAll)]
    public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10,
                                            [FromQuery] string? search = null)
    {
        var result= await _invoiceCacheService.GetPagedAsync(pageNumber, pageSize, search);
        return Ok(result);
    }

    [HttpGet(ApiRoutes.Invoices.GetById)]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var result= await _invoiceCacheService.GetByIdAsync(id);
        if (result is null)
            return NotFound($"Invoice with ID '{id}' not found in cache.");
        return Ok(result);
    }

    [HttpGet(ApiRoutes.Invoices.GetByClient)]
    public async Task<IActionResult> GetByClient([FromRoute] Guid clientId, 
                                                [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var result= await _invoiceCacheService.GetByClientIdAsync(clientId, pageNumber, pageSize);
        if (result is null)
            return NotFound($"Invoices for Client ID '{clientId}' not found in cache.");
        return Ok(result);
    }

    [HttpGet(ApiRoutes.Invoices.GetByStatus)]
    public async Task<IActionResult> GetByStatus([FromRoute] string status,
                                                [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        if (!Enum.TryParse<InvoiceStatus>(status, ignoreCase: true, out InvoiceStatus invoiceStatus))
            return BadRequest($"Invalid status value: '{status}'. Valid values: DRAFT, UNPAID, PAID, CANCELLED");
        var result= await _invoiceCacheService.GetByStatusAsync(invoiceStatus, pageNumber, pageSize);
        if (result is null)
            return NotFound($"Invoices with Status '{status}' not found in cache.");
        return Ok(result);
    }

}
