using ERP.PaymentService.Application.DTO;

namespace ERP.PaymentService.Infrastructure.Messaging.Events.Invoice;

public interface IInvoiceEventHandler
{
    Task HandleCreatedAsync(InvoiceEventDto dto);
    Task HandleCancelledAsync(InvoiceEventDto dto);
}