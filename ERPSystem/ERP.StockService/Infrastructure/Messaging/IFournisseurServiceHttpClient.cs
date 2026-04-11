using ERP.StockService.Application.DTOs;

namespace ERP.InvoiceService.Infrastructure.Messaging;

public interface IFournisseurServiceHttpClient
{
    Task<FournisseurResponseDto> GetByIdAsync(Guid id);
    Task<PagedResultDto<FournisseurResponseDto>> GetAllPagedAsync(int pageNumber = 1, int pageSize = 10);
}
