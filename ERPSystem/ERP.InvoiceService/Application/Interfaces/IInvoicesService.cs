using InvoiceService.Application.DTOs;
using InvoiceService.Domain;

namespace InvoiceService.Application.Interfaces
{

    public interface IInvoicesService
    {

        Task<InvoiceDto> GetByIdAsync(Guid id);
        Task<IEnumerable<InvoiceDto>> GetAllAsync(bool includeDeleted = false);
        Task<IEnumerable<InvoiceDto>> GetByClientIdAsync(Guid clientId);
        Task<IEnumerable<InvoiceDto>> GetByStatusAsync(InvoiceStatus status);
        Task<InvoiceDto> CreateAsync(CreateInvoiceDto dto);
        Task<InvoiceDto> AddItemAsync(Guid invoiceId, AddInvoiceItemDto dto);
        Task RemoveItemAsync(Guid invoiceId, Guid itemId);
        Task<InvoiceDto> FinalizeAsync(Guid id);
        Task<InvoiceDto> MarkAsPaidAsync(Guid id);
        Task<InvoiceDto> CancelAsync(Guid id);
        // ────────────────────────────────────────────────────────────────────────
        // SOFT DELETE OPERATIONS
        // ────────────────────────────────────────────────────────────────────────
        Task DeleteAsync(Guid id);

        Task RestoreAsync(Guid id);
        Task<InvoiceStatsDto> GetStatsAsync(int topClientsCount = 5);
    }
}