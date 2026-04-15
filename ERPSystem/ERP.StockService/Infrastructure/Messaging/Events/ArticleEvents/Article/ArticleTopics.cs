namespace ERP.StockService.Infrastructure.Messaging.Events.ArticleEvents.Article;

public static class ArticleTopics
{
    public const string Created = "article.created";
    public const string Updated = "article.updated";
    public const string Deleted = "article.deleted";
    public const string Restored = "article.restored";
}