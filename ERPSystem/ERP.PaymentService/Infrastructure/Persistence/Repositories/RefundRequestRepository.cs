using ERP.PaymentService.Application.DTO;
using ERP.PaymentService.Application.Interfaces;
using ERP.PaymentService.Domain;
using Microsoft.EntityFrameworkCore;

namespace ERP.PaymentService.Infrastructure.Persistence.Repositories
{
    public class RefundRequestRepository : IRefundRequestRepository
    {
        private readonly PaymentDbContext _context;

        public RefundRequestRepository(PaymentDbContext context)
        {
            _context = context;
        }

        public async Task<RefundRequest?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _context.Refunds
                .Include(r => r.Lines)
                .FirstOrDefaultAsync(r => r.Id == id, ct);
        }
        public async Task<RefundStatsDto> GetStatsAsync()
        {
            var stats = await _context.Refunds
                .GroupBy(_ => 1)
                .Select(g => new RefundStatsDto(
                    g.Count(),
                    g.Count(r => r.Status == RefundStatus.PENDING),
                    g.Count(r => r.Status == RefundStatus.COMPLETED)
                ))
                .FirstOrDefaultAsync();

            return stats ?? new RefundStatsDto(0, 0, 0);
        }
        public async Task<RefundRequest?> GetByInvoiceIdAsync(Guid invoiceId, CancellationToken ct = default)
        {
            return await _context.Refunds
                .Include(r => r.Lines)
                .FirstOrDefaultAsync(r => r.InvoiceId == invoiceId, ct);
        }

        public async Task<List<RefundRequest>> GetByClientIdAsync(Guid clientId)
        {
            return await _context.Refunds
                .Include(r => r.Lines)
                .Where(r => r.ClientId == clientId)
                .ToListAsync();
        }
        public async Task AddAsync(RefundRequest refund, CancellationToken ct = default)
        {
            await _context.Refunds.AddAsync(refund, ct);
        }

        public void Update(RefundRequest refund)
        {
            _context.Refunds.Update(refund);
        }

        public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
        {
            return await _context.Refunds
                .AnyAsync(r => r.Id == id, ct);
        }

        public Task SaveChangesAsync(CancellationToken ct = default)
        {
            return _context.SaveChangesAsync(ct);
        }
    }
}
