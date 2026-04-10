using ERP.StockService.Application.DTOs;

namespace ERP.StockService.Infrastructure.Messaging;

public interface IArticleServiceHttpClient
{
    Task<ArticleResponseDto> GetByIdAsync(Guid id);
    Task<PagedResultDto<ArticleResponseDto>> GetAllPagedAsync(int pageNumber = 1, int pageSize = 10);
}
