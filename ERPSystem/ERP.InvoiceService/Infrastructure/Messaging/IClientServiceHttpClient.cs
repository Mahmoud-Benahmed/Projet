using ERP.InvoiceService.Application.DTOs;

namespace ERP.InvoiceService.Infrastructure.Messaging;

public interface IClientServiceHttpClient
{
    Task<ClientResponseDto> GetByIdAsync(Guid id);
    Task<PagedResultDto<ClientResponseDto>> GetAllPagedAsync(int pageNumber = 1, int pageSize = 10);
}
