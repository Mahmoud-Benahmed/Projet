using InvoiceService.Domain;

namespace InvoiceService.Application.Interfaces
{
    public interface IInvoiceRepository
    {
        // ── Existing queries ─────────────────────────────────────────────────────
        Task<Invoice?> GetByIdAsync(Guid id);
        Task<Invoice?> GetByIdDeletedAsync(Guid id);
        Task<Invoice?> GetByIdWithItemsAsync(Guid id);
        Task<Invoice?> GetByInvoiceNumberAsync(string invoiceNumber);
        Task<IEnumerable<Invoice>> GetAllAsync(bool includeDeleted = false);
        Task<IEnumerable<Invoice>> GetByClientIdAsync(Guid clientId);
        Task<IEnumerable<Invoice>> GetByStatusAsync(InvoiceStatus status);
        Task AddAsync(Invoice invoice);
        Task UpdateAsync(Invoice invoice);
        Task<bool> ExistsByInvoiceNumberAsync(string invoiceNumber);

        // ── Stats queries ────────────────────────────────────────────────────────
        Task<IEnumerable<InvoiceStatProjection>> GetStatsProjectionAsync();
        Task<int> GetDeletedCountAsync();

        // ── Nested projection read-model ─────────────────────────────────────────
        public class InvoiceStatProjection
        {
            public Guid Id { get; init; }
            public InvoiceStatus Status { get; init; }
            public DateTime InvoiceDate { get; init; }
            public DateTime DueDate { get; init; }
            public decimal TotalHT { get; init; }
            public decimal TotalTTC { get; init; }
            public decimal TotalTVA { get; init; }
            public Guid ClientId { get; init; }
            public string ClientFullName { get; init; } = string.Empty;
        }
    }
}