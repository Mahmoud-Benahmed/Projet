using ERP.PaymentService.Application.DTO;
using ERP.PaymentService.Domain.LocalCache;

namespace ERP.PaymentService.Application.Interfaces.LocalCache;

public interface IInvoiceCacheService
{
    Task<InvoiceEventDto?> GetByIdAsync(Guid invoiceId);
    Task<PagedResultDto<InvoiceEventDto>> GetByClientIdAsync(Guid clientId, int pageNumber, int pageSize);
    Task<PagedResultDto<InvoiceEventDto>> GetByStatusAsync(InvoiceStatus status, int pageNumber, int pageSize);
    Task CreateAsync(InvoiceEventDto cache);
    Task<PagedResultDto<InvoiceEventDto>> GetPagedAsync(int pageNumber, int pageSize, string? search = null);


    Task SyncCreatedAsync(InvoiceEventDto dto);
    Task SyncCancelledAsync(InvoiceEventDto dto);
}
