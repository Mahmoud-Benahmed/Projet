using ERP.InvoiceService.Application.DTOs;

namespace ERP.InvoiceService.Infrastructure.Messaging.Events.ArticleEvents.ArticleCategory;

public interface IArticleCategoryEventHandler
{
    Task HandleCreatedAsync(ArticleCategoryResponseDto dto);
    Task HandleUpdatedAsync(ArticleCategoryResponseDto dto);
    Task HandleDeletedAsync(ArticleCategoryResponseDto dto);
    Task HandleRestoredAsync(ArticleCategoryResponseDto dto);
}