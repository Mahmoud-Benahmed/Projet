namespace ERP.InvoiceService.Infrastructure.Messaging.Events.ArticleEvents.Article
{
    public interface IArticleEventConsumer
    {
        Task StartAsync(CancellationToken cancellationToken);
    }
}
