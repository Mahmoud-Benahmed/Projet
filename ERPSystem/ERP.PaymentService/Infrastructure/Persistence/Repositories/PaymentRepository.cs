using ERP.PaymentService.Application.DTO;
using ERP.PaymentService.Application.Interfaces;
using ERP.PaymentService.Domain;
using Microsoft.EntityFrameworkCore;

namespace ERP.PaymentService.Infrastructure.Persistence.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly PaymentDbContext _context;

    public PaymentRepository(PaymentDbContext context)
    {
        _context = context;
    }

    public async Task<Payment?> GetByIdAsync(Guid id)
    {
        return await _context.Payments
            .Include(p => p.Allocations)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Payment?> GetByNumberAsync(string number)
    {
        return await _context.Payments
            .Include(p => p.Allocations)
            .FirstOrDefaultAsync(p => p.Number == number);
    }

    public async Task<List<Payment>> GetByClientIdAsync(Guid clientId)
    {
        return await _context.Payments
            .Include(p => p.Allocations)
            .Where(p => p.ClientId == clientId)
            .ToListAsync();
    }

    public async Task<(List<Payment> Items, int TotalCount)> GetPagedAsync(
        int pageNumber, int pageSize, string? search = null)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        IQueryable<Payment> query = _context.Payments
            .Include(p => p.Allocations)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            string q = search.Trim().ToLower();
            query = query.Where(p =>
                p.Number.ToLower().Contains(q) ||
                p.ClientId.ToString().Contains(q) ||
                p.ExternalReference != null && p.ExternalReference.ToLower().Contains(q));
        }

        int totalCount = await query.CountAsync();

        List<Payment> items = await query
            .OrderByDescending(p => p.PaymentDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<List<PaymentSummaryDto>> GetSummaryByInvoiceIdAsync(Guid invoiceId)
    {
        return await _context.PaymentsInvoices
            .Where(pi => pi.InvoiceId == invoiceId)
            .Join(
                _context.Payments,
                pi => pi.PaymentId,
                p => p.Id,
                (pi, p) => new
                {
                    Payment = p,
                    Allocation = pi.AmountAllocated
                }
            )
            .OrderByDescending(x => x.Payment.PaymentDate) // ✅ ORDER FIRST (translatable)
            .Select(x => new PaymentSummaryDto(
                PaymentId: x.Payment.Id,
                Number: x.Payment.Number,
                AmountAllocated: x.Allocation,
                Method: x.Payment.Method,
                PaymentDate: x.Payment.PaymentDate,
                ExternalReference: x.Payment.ExternalReference,
                Notes: x.Payment.Notes,
                IsCancelled: x.Payment.CancelledAt != null
            ))
            .ToListAsync();
    }


    public async Task AddAsync(Payment payment)
    {
        await _context.Payments.AddAsync(payment);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Payment payment)
    {
        var existing = await _context.Payments.FindAsync(payment.Id);

        if (existing is null)
            throw new InvalidOperationException(
                $"Payment with Id {payment.Id} not found.");

        _context.Entry(existing).CurrentValues.SetValues(payment);
        await _context.SaveChangesAsync();
    }
}