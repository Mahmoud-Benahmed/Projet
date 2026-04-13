namespace ERP.StockService.Infrastructure.Messaging.ArticleEvents.Category
{
    public interface ICategoryEventConsumer
    {
        Task StartAsync(CancellationToken cancellationToken);
    }
}
