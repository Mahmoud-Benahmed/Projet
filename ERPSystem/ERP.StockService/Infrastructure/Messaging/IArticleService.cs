namespace ERP.StockService.Infrastructure.Messaging;
public interface IArticleService
{
    Task ExistsByIdAsync(Guid id);
}
