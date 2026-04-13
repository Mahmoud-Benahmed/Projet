namespace ERP.StockService.Infrastructure.Messaging.ClientEvents.Client
{
    public interface IClientEventConsumer
    {
        Task StartAsync(CancellationToken cancellationToken);
    }
}
