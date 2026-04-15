namespace ERP.StockService.Infrastructure.Messaging.Events.InvoiceEvents;


public interface IInvoiceEventConsumer
{
    Task StartAsync(CancellationToken cancellationToken);
}
