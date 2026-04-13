namespace ERP.StockService.Infrastructure.Messaging.ArticleEvents.Article
{
    public interface IArticleEventConsumer
    {
        Task StartAsync(CancellationToken cancellationToken);
    }
}
