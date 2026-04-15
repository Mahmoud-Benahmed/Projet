namespace ERP.StockService.Infrastructure.Messaging.Events.ClientEvents.Client
{
    public interface IClientEventConsumer
    {
        Task StartAsync(CancellationToken cancellationToken);
    }
}
