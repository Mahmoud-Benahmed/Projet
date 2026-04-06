using ERP.InvoiceService.Application.DTOs;

namespace ERP.InvoiceService.Infrastructure.Messaging;
public interface IArticleServiceHttpClient
{
    Task<ArticleResponseDto> GetByIdAsync(Guid id);
}
