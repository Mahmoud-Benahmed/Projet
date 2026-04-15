namespace ERP.StockService.Infrastructure.Messaging.Events.ArticleEvents.Category
{
    public interface IArticleCategoryEventConsumer
    {
        Task StartAsync(CancellationToken cancellationToken);
    }
}
