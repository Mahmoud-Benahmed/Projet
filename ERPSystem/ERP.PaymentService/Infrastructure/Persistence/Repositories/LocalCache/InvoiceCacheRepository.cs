using ERP.PaymentService.Application.Interfaces.LocalCache;
using Microsoft.EntityFrameworkCore;

namespace ERP.PaymentService.Infrastructure.Persistence.Repositories.LocalCache;

public class InvoiceCacheRepository : IInvoiceCacheRepository
{
    private readonly PaymentDbContext _context;

    public InvoiceCacheRepository(PaymentDbContext context)
    {
        _context = context;
    }

    // ✅ fix 4: Id alone is sufficient — it's the primary key
    public async Task<InvoiceCache?> GetByIdAsync(Guid invoiceId)
    {
        return await _context.InvoiceCaches
            .FirstOrDefaultAsync(ic => ic.Id == invoiceId);
    }

    public async Task<(List<InvoiceCache> Items, int TotalCount)> GetByClientIdAsync(
        Guid clientId, int pageNumber, int pageSize)
    {
        // ✅ fix 2: guard clamps
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        IQueryable<InvoiceCache> query = _context.InvoiceCaches
            .AsNoTracking()                          // ✅ fix 1
            .Where(ic => ic.ClientId == clientId)
            .OrderByDescending(ic => ic.LastUpdated); // ✅ fix 3

        int totalCount = await query.CountAsync();

        List<InvoiceCache> items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<(List<InvoiceCache> Items, int TotalCount)> GetByStatusAsync(
        InvoiceStatus status, int pageNumber, int pageSize)
    {
        // ✅ fix 2: guard clamps
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        IQueryable<InvoiceCache> query = _context.InvoiceCaches
            .AsNoTracking()                          // ✅ fix 1
            .Where(ic => ic.Status == status)
            .OrderByDescending(ic => ic.LastUpdated); // ✅ fix 3

        int totalCount = await query.CountAsync();

        List<InvoiceCache> items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task AddAsync(InvoiceCache cache)
    {
        await _context.InvoiceCaches.AddAsync(cache);
        await _context.SaveChangesAsync();
    }

    public async Task SaveChangesAsync(InvoiceCache cache)
    {
        var existing = await _context.InvoiceCaches.FindAsync(cache.Id);

        if (existing is null)
            throw new InvalidOperationException(
                $"InvoiceCache with Id {cache.Id} not found. Use AddAsync to insert new entries.");

        _context.Entry(existing).CurrentValues.SetValues(cache);
        await _context.SaveChangesAsync();
    }

    public async Task<(List<InvoiceCache> Items, int TotalCount)> GetPagedAsync(
        int pageNumber, int pageSize, string? search = null)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        IQueryable<InvoiceCache> query = _context.InvoiceCaches
            .AsNoTracking()
            .OrderByDescending(ic => ic.LastUpdated);

        if (!string.IsNullOrWhiteSpace(search))
        {
            string q = search.Trim().ToLower();
            query = query.Where(ic =>
                ic.InvoiceNumber.ToLower().Contains(q) ||
                ic.ClientId.ToString().Contains(q));
        }

        int totalCount = await query.CountAsync();

        List<InvoiceCache> items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}