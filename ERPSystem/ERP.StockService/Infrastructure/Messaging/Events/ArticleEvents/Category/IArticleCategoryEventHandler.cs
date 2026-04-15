using ERP.StockService.Application.DTOs;

namespace ERP.StockService.Infrastructure.Messaging.Events.ArticleEvents.Category;

public interface IArticleCategoryEventHandler
{
    Task HandleCreatedAsync(ArticleCategoryResponseDto dto);
    Task HandleUpdatedAsync(ArticleCategoryResponseDto dto);
    Task HandleDeletedAsync(ArticleCategoryResponseDto dto);
    Task HandleRestoredAsync(ArticleCategoryResponseDto dto);
}