namespace ERP.StockService.Infrastructure.Messaging.Events.ClientEvents.Category
{
    public interface IClientCategoryEventConsumer
    {
        Task StartAsync(CancellationToken cancellationToken);
    }
}
