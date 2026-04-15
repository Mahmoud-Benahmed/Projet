namespace ERP.InvoiceService.Infrastructure.Messaging.Events.ArticleEvents.ArticleCategory
{
    public interface IArticleCategoryEventConsumer
    {
        Task StartAsync(CancellationToken cancellationToken);
    }
}
