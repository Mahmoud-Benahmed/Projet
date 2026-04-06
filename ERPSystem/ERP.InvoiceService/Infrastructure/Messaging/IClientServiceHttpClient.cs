using ERP.InvoiceService.Application.DTOs;

namespace ERP.InvoiceService.Infrastructure.Messaging;
public interface IClientServiceHttpClient
{
    Task<ClientResponseDto> GetByIdAsync(Guid id);
}
