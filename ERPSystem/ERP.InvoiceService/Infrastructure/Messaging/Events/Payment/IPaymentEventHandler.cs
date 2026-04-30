using ERP.InvoiceService.Application.DTOs;

namespace ERP.InvoiceService.Infrastructure.Messaging.Events.Payment;

public interface IPaymentEventHandler
{
    Task HandleInvoicePaidAsync(InvoicePaidEvent eventDto);
    Task HandlePaymentCancelledAsync(PaymentCancelledEvent eventDto);

}