using ERP.StockService.Application.DTOs;

namespace ERP.StockService.Infrastructure.Messaging.ArticleEvents.Article;

public interface IArticleEventHandler
{
    Task HandleCreatedAsync(ArticleResponseDto dto);
    Task HandleUpdatedAsync(ArticleResponseDto dto);
    Task HandleDeletedAsync(ArticleResponseDto dto);
    Task HandleRestoredAsync(ArticleResponseDto dto);
}