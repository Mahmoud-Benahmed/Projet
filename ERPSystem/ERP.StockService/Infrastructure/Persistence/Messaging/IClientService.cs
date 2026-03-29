namespace ERP.StockService.Infrastructure.Persistence.Messaging;
public interface IClientService
{
    Task ExistsByIdAsync(Guid id);
}
