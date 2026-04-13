namespace ERP.InvoiceService.Infrastructure.Messaging.ClientEvents.Category
{
    public interface IClientCategoryEventConsumer
    {
        Task StartAsync(CancellationToken cancellationToken);
    }
}
