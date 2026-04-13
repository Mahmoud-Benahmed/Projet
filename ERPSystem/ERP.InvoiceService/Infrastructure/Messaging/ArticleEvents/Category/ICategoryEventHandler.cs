using ERP.InvoiceService.Application.DTOs;

namespace ERP.InvoiceService.Infrastructure.Messaging.ArticleEvents.Category;

public interface ICategoryEventHandler
{
    Task HandleCreatedAsync(CategoryResponseDto dto);
    Task HandleUpdatedAsync(CategoryResponseDto dto);
    Task HandleDeletedAsync(CategoryResponseDto dto);
    Task HandleRestoredAsync(CategoryResponseDto dto);
}