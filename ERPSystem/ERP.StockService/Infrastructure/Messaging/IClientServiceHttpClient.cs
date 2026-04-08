using ERP.StockService.Application.DTOs;

namespace ERP.StockService.Infrastructure.Messaging;

public interface IClientServiceHttpClient
{
    Task<ClientResponseDto> GetByIdAsync(Guid id);
    Task<PagedResultDto<ClientResponseDto>> GetAllPagedAsync(int pageNumber = 1, int pageSize = 10);
}
