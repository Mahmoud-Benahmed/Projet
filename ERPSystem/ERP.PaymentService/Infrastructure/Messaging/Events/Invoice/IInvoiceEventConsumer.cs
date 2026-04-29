namespace ERP.PaymentService.Infrastructure.Messaging.Events.Invoice;

public interface IInvoiceEventConsumer
{
    Task StartAsync(CancellationToken cancellationToken);
}
