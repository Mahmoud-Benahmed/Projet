using ERP.PaymentService.Application.Interfaces;
using ERP.PaymentService.Domain.LocalCache;
using Microsoft.EntityFrameworkCore;

namespace ERP.PaymentService.Infrastructure.Persistence.LocalCache.InvoiceCache
{
    public class InvoiceCacheRepository : IInvoiceCacheRepository
    {
        private readonly PaymentDbContext _context;

        public InvoiceCacheRepository(PaymentDbContext context)
        {
            _context = context;
        }

        public async Task<Invoice?> GetByIdAsync(Guid invoiceId)
        {
            return await _context.InvoiceCache
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);
        }

        public async Task UpsertAsync(Invoice invoice)
        {
            var existing = await _context.InvoiceCache
                .FirstOrDefaultAsync(i => i.InvoiceId == invoice.InvoiceId);

            if (existing is null)
            {
                await _context.InvoiceCache.AddAsync(invoice);
            }
            else
            {
                existing.ClientId = invoice.ClientId;
                existing.TotalTTC = invoice.TotalTTC;
                existing.TotalPaid = invoice.TotalPaid;
                existing.DueDate = invoice.DueDate;
                existing.InvoiceDate = invoice.InvoiceDate;
                existing.Status = invoice.Status;
                existing.LateFeeApplied = invoice.LateFeeApplied;
                existing.LateFeeAmount = invoice.LateFeeAmount;

                _context.InvoiceCache.Update(existing);
            }

            await _context.SaveChangesAsync();
        }

        public async Task UpdateStatusAsync(Guid invoiceId, string status)
        {
            var invoice = await _context.InvoiceCache
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

            if (invoice is null)
                return;

            invoice.Status = status;
            _context.InvoiceCache.Update(invoice);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateTotalPaidAsync(Guid invoiceId, decimal totalPaid)
        {
            var invoice = await _context.InvoiceCache
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

            if (invoice is null)
                return;

            invoice.TotalPaid = totalPaid;
            _context.InvoiceCache.Update(invoice);
            await _context.SaveChangesAsync();
        }
    }
}
