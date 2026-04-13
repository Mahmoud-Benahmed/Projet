namespace ERP.StockService.Infrastructure.Messaging.FournisseurEvents;

public interface IFournisseurEventConsumer
{
    Task StartAsync(CancellationToken cancellationToken);
}
