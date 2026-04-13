namespace ERP.InvoiceService.Infrastructure.Messaging.ArticleEvents.Article
{
    public interface IArticleEventConsumer
    {
        Task StartAsync(CancellationToken cancellationToken);
    }
}
