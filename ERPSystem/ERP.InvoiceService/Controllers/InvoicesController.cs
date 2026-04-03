using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using InvoiceService.Application.DTOs;
using InvoiceService.Application.Interfaces;
using InvoiceService.Domain;
using ERP.InvoiceService.Controllers;

namespace InvoiceService.API.Controllers
{
    [ApiController]
    public class InvoicesController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;
        public InvoicesController(IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }
        // ════════════════════════════════════════════════════════════════════════════
        // GET OPERATIONS
        // ════════════════════════════════════════════════════════════════════════════
        [HttpGet(ApiRoutes.Invoices.GetAll)]
        public async Task<IActionResult> GetAll([FromQuery] bool includeDeleted = false)
        {
            var invoices = await _invoiceService.GetAllAsync(includeDeleted);
            return Ok(invoices);
        }
        [HttpGet(ApiRoutes.Invoices.GetById)]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var invoice = await _invoiceService.GetByIdAsync(id);
            return Ok(invoice);
        }
        [HttpGet(ApiRoutes.Invoices.GetByClient)]
        public async Task<IActionResult> GetByClient([FromRoute] Guid clientId)
        {
            var invoices = await _invoiceService.GetByClientIdAsync(clientId);
            return Ok(invoices);
        }
        [HttpGet(ApiRoutes.Invoices.GetByStatus)]
        public async Task<IActionResult> GetByStatus([FromRoute] string status)
        {
            if (!Enum.TryParse<InvoiceStatus>(status, ignoreCase: true, out var invoiceStatus))
                return BadRequest($"Invalid status value: '{status}'. Valid values: DRAFT, UNPAID, PAID, CANCELLED");

            var invoices = await _invoiceService.GetByStatusAsync(invoiceStatus);
            return Ok(invoices);
        }
        // ════════════════════════════════════════════════════════════════════════════
        // POST OPERATIONS - Create/Add
        // ════════════════════════════════════════════════════════════════════════════
        [HttpPost(ApiRoutes.Invoices.Create)]
        public async Task<IActionResult> Create([FromBody] CreateInvoiceDto dto)
        {
            var invoice = await _invoiceService.CreateAsync(dto);
            return CreatedAtAction(
                nameof(GetById),
                new { id = invoice.Id },
                invoice);
        }
        [HttpPost(ApiRoutes.Invoices.AddItem)]
        public async Task<IActionResult> AddItem([FromRoute] Guid id, [FromBody] AddInvoiceItemDto dto)
        {
            var invoice = await _invoiceService.AddItemAsync(id, dto);
            return Ok(invoice);
        }

        // ════════════════════════════════════════════════════════════════════════════
        // DELETE OPERATIONS
        // ════════════════════════════════════════════════════════════════════════════
        [HttpDelete(ApiRoutes.Invoices.RemoveItem)]
        public async Task<IActionResult> RemoveItem([FromRoute] Guid id, [FromRoute] Guid itemId)
        {
            await _invoiceService.RemoveItemAsync(id, itemId);
            return NoContent();
        }
        [HttpPut(ApiRoutes.Invoices.Finalize)]
        public async Task<IActionResult> Finalize([FromRoute] Guid id)
        {
            var invoice = await _invoiceService.FinalizeAsync(id);
            return Ok(invoice);
        }
        [HttpPut(ApiRoutes.Invoices.MarkAsPaid)]
        public async Task<IActionResult> MarkAsPaid([FromRoute] Guid id)
        {
            var invoice = await _invoiceService.MarkAsPaidAsync(id);
            return Ok(invoice);
        }
        [HttpPut(ApiRoutes.Invoices.Cancel)]
        public async Task<IActionResult> Cancel([FromRoute] Guid id)
        {
            var invoice = await _invoiceService.CancelAsync(id);
            return Ok(invoice);
        }
        // ════════════════════════════════════════════════════════════════════════════
        // SOFT DELETE OPERATIONS
        // ════════════════════════════════════════════════════════════════════════════
        [HttpDelete(ApiRoutes.Invoices.Delete)]
        public async Task<IActionResult> Delete([FromRoute] Guid id)
        {
            await _invoiceService.DeleteAsync(id);
            return NoContent();
        }
        [HttpPut(ApiRoutes.Invoices.Restore)]
        public async Task<IActionResult> Restore([FromRoute] Guid id)
        {
            await _invoiceService.RestoreAsync(id);
            return NoContent();
        }
    }
}