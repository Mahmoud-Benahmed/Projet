namespace ERP.StockService.Infrastructure.Persistence.Messaging;
public interface IArticleService
{
    Task ExistsByIdAsync(Guid id);
}
