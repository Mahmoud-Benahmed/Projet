namespace ERP.InvoiceService.Infrastructure.Messaging.Events.ClientEvents.Category
{
    public interface IClientCategoryEventConsumer
    {
        Task StartAsync(CancellationToken cancellationToken);
    }
}
