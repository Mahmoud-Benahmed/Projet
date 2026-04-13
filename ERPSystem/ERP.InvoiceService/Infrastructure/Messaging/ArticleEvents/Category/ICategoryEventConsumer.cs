namespace ERP.InvoiceService.Infrastructure.Messaging.ArticleEvents.Category
{
    public interface ICategoryEventConsumer
    {
        Task StartAsync(CancellationToken cancellationToken);
    }
}
