using InvoiceService.Application.DTOs;
using InvoiceService.Domain;

namespace InvoiceService.Application.Interfaces
{

    public interface IInvoicesService
    {

        Task<InvoiceDto> GetByIdAsync(Guid id);
        Task<PagedResultDto<InvoiceDto>> GetAllAsync(int pageNumber, int pageSize, bool includeDeleted = false);
        Task<PagedResultDto<InvoiceDto>> GetByClientIdAsync(Guid clientId, int pageNumber, int pageSize);
        Task<PagedResultDto<InvoiceDto>> GetByStatusAsync(InvoiceStatus status, int pageNumber, int pageSize);
        Task<InvoiceDto> CreateAsync(CreateInvoiceDto dto);
        Task<InvoiceDto> UpdateAsync(Guid id, UpdateInvoiceDto dto);
        Task MarkAsUnpaidAsync(Guid id);

        Task<InvoiceDto> AddItemAsync(Guid invoiceId, AddInvoiceItemDto dto);
        Task RemoveItemAsync(Guid invoiceId, Guid itemId);
        Task<InvoiceDto> FinalizeAsync(Guid id);
        Task<InvoiceDto> MarkAsPaidAsync(Guid id, decimal? paidAmount= null, DateTime? paidAt= null);
        Task<InvoiceDto> CancelAsync(Guid id);
        // ────────────────────────────────────────────────────────────────────────
        // SOFT DELETE OPERATIONS
        // ────────────────────────────────────────────────────────────────────────
        Task DeleteAsync(Guid id);

        Task RestoreAsync(Guid id);
        Task<InvoiceStatsDto> GetStatsAsync(int topClientsCount = 5);
    }
}