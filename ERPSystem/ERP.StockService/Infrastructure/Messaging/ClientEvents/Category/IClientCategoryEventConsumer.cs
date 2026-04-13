namespace ERP.StockService.Infrastructure.Messaging.ClientEvents.Category
{
    public interface IClientCategoryEventConsumer
    {
        Task StartAsync(CancellationToken cancellationToken);
    }
}
