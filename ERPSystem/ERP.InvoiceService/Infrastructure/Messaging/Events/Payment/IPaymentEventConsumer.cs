namespace ERP.InvoiceService.Infrastructure.Messaging.Events.Payment;

public interface IPaymentEventConsumer
{
    Task StartAsync(CancellationToken cancellationToken);
}
