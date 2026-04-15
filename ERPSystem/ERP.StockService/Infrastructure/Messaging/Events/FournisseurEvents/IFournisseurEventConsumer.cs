namespace ERP.StockService.Infrastructure.Messaging.Events.FournisseurEvents;

public interface IFournisseurEventConsumer
{
    Task StartAsync(CancellationToken cancellationToken);
}
