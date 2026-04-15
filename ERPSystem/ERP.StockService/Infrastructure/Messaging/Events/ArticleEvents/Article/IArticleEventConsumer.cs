namespace ERP.StockService.Infrastructure.Messaging.Events.ArticleEvents.Article
{
    public interface IArticleEventConsumer
    {
        Task StartAsync(CancellationToken cancellationToken);
    }
}
