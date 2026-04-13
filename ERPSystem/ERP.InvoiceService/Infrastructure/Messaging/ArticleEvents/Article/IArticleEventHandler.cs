using ERP.InvoiceService.Application.DTOs;

namespace ERP.InvoiceService.Infrastructure.Messaging.ArticleEvents.Article;

public interface IArticleEventHandler
{
    Task HandleCreatedAsync(ArticleResponseDto dto);
    Task HandleUpdatedAsync(ArticleResponseDto dto);
    Task HandleDeletedAsync(ArticleResponseDto dto);
    Task HandleRestoredAsync(ArticleResponseDto dto);
}