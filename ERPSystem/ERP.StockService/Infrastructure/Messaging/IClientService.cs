namespace ERP.StockService.Infrastructure.Messaging;
public interface IClientService
{
    Task ExistsByIdAsync(Guid id);
}
