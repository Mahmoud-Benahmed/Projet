using ERP.PaymentService.Application.Interfaces;
using ERP.PaymentService.Domain.Entities;
using ERP.PaymentService.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERP.PaymentService.Infrastructure.Persistence
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly PaymentDbContext _context;

        public PaymentRepository(PaymentDbContext context)
        {
            _context = context;
        }

        public async Task<List<Payment>> GetAllAsync()
        {
            return await _context.Payments
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Payment?> GetByIdAsync(Guid id)
        {
            return await _context.Payments
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<List<Payment>> GetByInvoiceIdAsync(Guid invoiceId)
        {
            return await _context.Payments
                .AsNoTracking()
                .Where(p => p.InvoiceId == invoiceId)
                .ToListAsync();
        }

        public async Task<List<Payment>> GetByClientIdAsync(Guid clientId)
        {
            return await _context.Payments
                .AsNoTracking()
                .Where(p => p.ClientId == clientId)
                .ToListAsync();
        }

        public async Task<List<Payment>> GetByStatusAsync(PaymentStatus status)
        {
            return await _context.Payments
                .AsNoTracking()
                .Where(p => p.Status == status)
                .ToListAsync();
        }

        public async Task<List<Payment>> GetCompletedByInvoiceIdAsync(Guid invoiceId)
        {
            return await _context.Payments
                .AsNoTracking()
                .Where(p => p.InvoiceId == invoiceId
                         && p.Status == PaymentStatus.COMPLETED
                         && !p.IsDeleted)
                .ToListAsync();
        }

        public async Task AddAsync(Payment payment)
        {
            await _context.Payments.AddAsync(payment);
        }

        public async Task UpdateAsync(Payment payment)
        {
            _context.Payments.Update(payment);
            await Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
