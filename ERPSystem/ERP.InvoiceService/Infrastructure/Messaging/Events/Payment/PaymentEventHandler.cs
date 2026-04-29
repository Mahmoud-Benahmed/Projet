using ERP.InvoiceService.Application.DTOs;
using InvoiceService.Application.Interfaces;
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
}