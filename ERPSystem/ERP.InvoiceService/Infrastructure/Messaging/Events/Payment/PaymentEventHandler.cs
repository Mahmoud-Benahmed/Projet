using ERP.InvoiceService.Application.DTOs;
using InvoiceService.Application.Interfaces;
using InvoiceService.Domain;
namespace ERP.InvoiceService.Infrastructure.Messaging.Events.Payment;

public sealed class PaymentEventHandler : IPaymentEventHandler
{
    private readonly IInvoicesService _invoiceService;
    private readonly ILogger<PaymentEventHandler> _logger;

    public PaymentEventHandler(IInvoicesService invoicesService, ILogger<PaymentEventHandler> logger)
    {
        _invoiceService = invoicesService;
        _logger = logger;
    }

    public async Task HandleInvoicePaidAsync(InvoicePaidEvent eventDto)
    {
        try
        {
            _logger.LogInformation(
                "Invoice {InvoiceId} fully paid via Payment {PaymentId} | AmountPaid: {AmountPaid} | PaidAt: {PaidAt}",
                eventDto.InvoiceId,
                eventDto.PaymentId,
                eventDto.PaidAmount,
                eventDto.PaidAt
            );

            await _invoiceService.MarkAsPaidAsync(eventDto.InvoiceId, eventDto.PaidAmount, eventDto.PaidAt);
        }
        catch (Exception ex)
        {
            // Log what we DO have access to — the event data itself
            _logger.LogError(ex,
                "Error handling InvoicePaidEvent — InvoiceId: {InvoiceId}, PaymentId: {PaymentId}",
                eventDto.InvoiceId,
                eventDto.PaymentId);

            throw; // ✅ re-throw so the consumer catches it and doesn't commit the offset
        }
    }

    public async Task HandlePaymentCancelledAsync(PaymentCancelledEvent eventDto)
    {
        try
        {
            _logger.LogInformation(
                "Handling PaymentCancelledEvent — PaymentId: {PaymentId}, " +
                "InvoiceId: {InvoiceId}, ReversedAmount: {Amount}",
                eventDto.PaymentId,
                eventDto.InvoiceId,
                eventDto.ReversedAmount);

            var invoice = await _invoiceService.GetByIdAsync(eventDto.InvoiceId);

            if (invoice is null)
            {
                _logger.LogWarning(
                    "Invoice {InvoiceId} not found while handling PaymentCancelledEvent {PaymentId}. Skipping.",
                    eventDto.InvoiceId, eventDto.PaymentId);
                return;
            }

            // Cancelled invoices should not be touched regardless
            if (invoice.Status == InvoiceStatus.CANCELLED.ToString())
            {
                _logger.LogWarning(
                    "Invoice {InvoiceId} is already CANCELLED. Skipping payment reversal.",
                    eventDto.InvoiceId);
                return;
            }

            // If fully paid, mark back to unpaid
            if (invoice.Status == InvoiceStatus.PAID.ToString())
            {
                await _invoiceService.MarkAsUnpaidAsync(eventDto.InvoiceId);

                _logger.LogInformation(
                    "Invoice {InvoiceId} marked back as UNPAID after payment {PaymentId} was cancelled.",
                    eventDto.InvoiceId, eventDto.PaymentId);
            }
            // Partial payment cancellation — invoice was already UNPAID,
            // no status change needed but log it for audit
            else
            {
                _logger.LogInformation(
                    "Invoice {InvoiceId} was UNPAID (partial payment). " +
                    "No status change needed after cancellation of payment {PaymentId}.",
                    eventDto.InvoiceId, eventDto.PaymentId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error handling PaymentCancelledEvent — InvoiceId: {InvoiceId}, PaymentId: {PaymentId}",
                eventDto.InvoiceId, eventDto.PaymentId);

            throw; // re-throw so the consumer doesn't commit the offset
        }
    }
}