using InvoiceService.Application.Interfaces;
using InvoiceService.Domain;
using Microsoft.EntityFrameworkCore;
using static InvoiceService.Application.Interfaces.IInvoiceRepository;

namespace ERP.InvoiceService.Infrastructure.Persistence
{


    public interface IInvoiceNumberGenerator
    {
        /// <summary>
        /// Generates the next invoice number in format INV-YYYY-SEQ
        /// This method is thread-safe and should be called within a transaction
        /// </summary>
        Task<string> GenerateNextInvoiceNumberAsync();
    }


    public class InvoiceRepository : IInvoiceRepository
    {
        private readonly InvoiceDbContext _context;

        public InvoiceRepository(InvoiceDbContext context)
        {
            _context = context;
        }

        // ── Existing queries ─────────────────────────────────────────────────────

        public async Task<Invoice?> GetByIdAsync(Guid id)
        {
            return await _context.Invoices
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<Invoice?> GetByIdDeletedAsync(Guid id)
        {
            return await _context.Invoices.IgnoreQueryFilters()
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<Invoice?> GetByIdWithItemsAsync(Guid id)
        {
            return await _context.Invoices
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<Invoice?> GetByInvoiceNumberAsync(string invoiceNumber)
        {
            return await _context.Invoices
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber);
        }

        public async Task<IEnumerable<Invoice>> GetAllAsync(bool includeDeleted = false)
        {
            Console.WriteLine($"includeDeleted: {includeDeleted}");
            var query = includeDeleted
                ? _context.Invoices.IgnoreQueryFilters()
                : _context.Invoices.AsQueryable();

            return await query
                .Include(i => i.Items)
                .ToListAsync();
        }

        public async Task<IEnumerable<Invoice>> GetByClientIdAsync(Guid clientId)
        {
            return await _context.Invoices
                .Include(i => i.Items)
                .Where(i => i.ClientId == clientId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Invoice>> GetByStatusAsync(InvoiceStatus status)
        {
            return await _context.Invoices
                .Include(i => i.Items)
                .Where(i => i.Status == status)
                .ToListAsync();
        }

        public async Task AddAsync(Invoice invoice)
        {
            await _context.Invoices.AddAsync(invoice);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Invoice invoice)
        {
            _context.Invoices.Update(invoice);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsByInvoiceNumberAsync(string invoiceNumber)
        {
            return await _context.Invoices
                .IgnoreQueryFilters()
                .AnyAsync(i => i.InvoiceNumber == invoiceNumber);
        }

        // ── Stats queries ─────────────────────────────────────────────────────────

        /// <summary>
        /// Projects only the header-level fields needed for stats aggregation.
        /// Items are intentionally excluded — no Include() — keeping the query lean.
        /// The global soft-delete query filter applies automatically (non-deleted only).
        /// </summary>
        public async Task<IEnumerable<InvoiceStatProjection>> GetStatsProjectionAsync()
        {
            return await _context.Invoices
                .AsNoTracking()
                .Select(i => new InvoiceStatProjection
                {
                    Id = i.Id,
                    Status = i.Status,
                    InvoiceDate = i.InvoiceDate,
                    DueDate = i.DueDate,
                    TotalHT = i.TotalHT,
                    TotalTTC = i.TotalTTC,
                    TotalTVA = i.TotalTVA,
                    ClientId = i.ClientId,
                    ClientFullName = i.ClientFullName
                })
                .ToListAsync();
        }

        /// <summary>
        /// Counts soft-deleted invoices.
        /// IgnoreQueryFilters() is required to lift the global IsDeleted filter.
        /// </summary>
        public async Task<int> GetDeletedCountAsync()
        {
            return await _context.Invoices
                .IgnoreQueryFilters()
                .CountAsync(i => i.IsDeleted);
        }
    }
}