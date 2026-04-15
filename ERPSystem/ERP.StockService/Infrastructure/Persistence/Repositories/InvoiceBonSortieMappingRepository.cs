using ERP.StockService.Domain;
using Microsoft.EntityFrameworkCore;

namespace ERP.StockService.Infrastructure.Persistence.Repositories;

public class InvoiceBonSortieMappingRepository : IInvoiceBonSortieMappingRepository
{
    private readonly StockDbContext _context;

    public InvoiceBonSortieMappingRepository(StockDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(InvoiceBonSortieMapping mapping)
    {
        await _context.InvoiceBonSortieMappings.AddAsync(mapping);
    }

    public async Task<Guid?> GetBonSortieIdByInvoiceIdAsync(Guid invoiceId)
    {
        var mapping = await _context.InvoiceBonSortieMappings
            .FirstOrDefaultAsync(m => m.InvoiceId == invoiceId);
        return mapping?.BonSortieId;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}