using ERP.PaymentService.Application.DTOs.Payment;
using ERP.PaymentService.Application.Exceptions;
using ERP.PaymentService.Application.Interfaces;
using ERP.PaymentService.Domain.Enums;
using ERP.PaymentService.Properties;
using Microsoft.AspNetCore.Mvc;

namespace ERP.PaymentService.Controllers
{
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentsService _paymentsService;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(
            ILogger<PaymentsController> logger,
            IPaymentsService paymentsService)
        {
            _logger = logger;
            _paymentsService = paymentsService;
        }

        // GET ENDPOINTS
        [HttpGet(ApiRoutes.Payments.GetAll)]
        public async Task<IActionResult> GetAllAsync()
        {
            var payments = await _paymentsService.GetAllAsync();
            return Ok(payments);
        }

        [HttpGet(ApiRoutes.Payments.GetById)]
        public async Task<IActionResult> GetByIdAsync(Guid id)
        {
            try
            {
                var payment = await _paymentsService.GetByIdAsync(id);
                return Ok(payment);
            }
            catch (PaymentNotFoundException ex)
            {
                _logger.LogWarning(ex, "\n\nPayment {PaymentId} not found\n\n", id);
                return NotFound(ex.Message);
            }
        }

        [HttpGet(ApiRoutes.Payments.GetByInvoice)]
        public async Task<IActionResult> GetByInvoiceIdAsync(Guid invoiceId)
        {
            var payments = await _paymentsService.GetByInvoiceIdAsync(invoiceId);
            return Ok(payments);
        }

        [HttpGet(ApiRoutes.Payments.GetByClient)]
        public async Task<IActionResult> GetByClientIdAsync(Guid clientId)
        {
            var payments = await _paymentsService.GetByClientIdAsync(clientId);
            return Ok(payments);
        }

        [HttpGet(ApiRoutes.Payments.GetByStatus)]
        public async Task<IActionResult> GetByStatusAsync(PaymentStatus status)
        {
            var payments = await _paymentsService.GetByStatusAsync(status);
            return Ok(payments);
        }

        [HttpGet(ApiRoutes.Payments.GetStats)]
        public async Task<IActionResult> GetStatsAsync()
        {
            var stats = await _paymentsService.GetStatsAsync();
            return Ok(stats);
        }

        // INVOICE-SCOPED ENDPOINTS
        [HttpGet(ApiRoutes.Invoices.GetPaymentSummary)]
        public async Task<IActionResult> GetPaymentSummaryAsync(Guid id)
        {
            try
            {
                var summary = await _paymentsService.GetPaymentSummaryAsync(id);
                return Ok(summary);
            }
            catch (InvoiceNotFoundException ex)
            {
                _logger.LogWarning(ex, "\n\nInvoice {InvoiceId} not found for payment summary\n\n", id);
                return NotFound(ex.Message);
            }
        }

        [HttpGet(ApiRoutes.Invoices.GetPaymentsByInvoice)]
        public async Task<IActionResult> GetPaymentsByInvoiceAsync(Guid invoiceId)
        {
            var payments = await _paymentsService.GetByInvoiceIdAsync(invoiceId);
            return Ok(payments);
        }

        // COMMAND ENDPOINTS
        [HttpPost(ApiRoutes.Payments.Create)]
        public async Task<IActionResult> CreateAsync([FromBody] CreatePaymentDto dto)
        {
            try
            {
                var payment = await _paymentsService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetByIdAsync), new { id = payment.Id }, payment);
            }
            catch (InvoiceNotFoundException ex)
            {
                _logger.LogWarning(ex, "\n\nInvoice not found during payment creation\n\n");
                return NotFound(ex.Message);
            }
            catch (InvoiceAlreadyPaidException ex)
            {
                _logger.LogWarning(ex, "\n\nInvoice already paid\n\n");
                return BadRequest(ex.Message);
            }
            catch (InvoiceCancelledException ex)
            {
                _logger.LogWarning(ex, "\n\nInvoice is cancelled\n\n");
                return BadRequest(ex.Message);
            }
            catch (ClientBlockedException ex)
            {
                _logger.LogWarning(ex, "\n\nClient is blocked\n\n");
                return BadRequest(ex.Message);
            }
        }

        [HttpPut(ApiRoutes.Payments.Update)]
        public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] UpdatePaymentDto dto)
        {
            try
            {
                await _paymentsService.UpdateAsync(id, dto);
                return NoContent();
            }
            catch (PaymentNotFoundException ex)
            {
                _logger.LogWarning(ex, "\n\nPayment {PaymentId} not found for update\n\n", id);
                return NotFound(ex.Message);
            }
        }

        [HttpDelete(ApiRoutes.Payments.Delete)]
        public async Task<IActionResult> DeleteAsync(Guid id)
        {
            try
            {
                await _paymentsService.DeleteAsync(id);
                return NoContent();
            }
            catch (PaymentNotFoundException ex)
            {
                _logger.LogWarning(ex, "\n\nPayment {PaymentId} not found for deletion\n\n", id);
                return NotFound(ex.Message);
            }
        }

        [HttpPut(ApiRoutes.Payments.Restore)]
        public async Task<IActionResult> RestoreAsync(Guid id)
        {
            try
            {
                await _paymentsService.RestoreAsync(id);
                return NoContent();
            }
            catch (PaymentNotFoundException ex)
            {
                _logger.LogWarning(ex, "\n\nPayment {PaymentId} not found for restore\n\n", id);
                return NotFound(ex.Message);
            }
        }
    }
}