using ERP.InvoiceService.Application.DTOs;

namespace ERP.InvoiceService.Infrastructure.Messaging;

public interface IArticleServiceHttpClient
{
    Task<ArticleResponseDto> GetByIdAsync(Guid id);
    Task<PagedResultDto<ArticleResponseDto>> GetAllPagedAsync(int pageNumber = 1, int pageSize = 10);
}
