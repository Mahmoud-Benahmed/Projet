using ERP.StockService.Domain;
using Microsoft.EntityFrameworkCore;

namespace ERP.StockService.Infrastructure.Persistence.Repositories;

public class JournalStockRepository: IJournalStockRepository
{
    private readonly StockDbContext _context;
    public JournalStockRepository(StockDbContext context) => _context = context;

    public async Task AddAsync(JournalStock entry) 
        => await _context.JournalStocks.AddAsync(entry);

    public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
    public async Task<List<JournalStock>> GetByArticleAsync(Guid articleId) 
        => await _context.JournalStocks.Where(js => js.ArticleId == articleId).ToListAsync();

    public async Task DeleteAsync(JournalStock entry)
    {
        _context.JournalStocks.Remove(entry);
        await _context.SaveChangesAsync();
    }

    // In your JournalStockRepository
    public async Task<decimal> GetCurrentStockAsync(Guid articleId)
    {
        // Returns 0 if no movements exist yet for this article
        var movements = await _context.JournalStocks
            .Where(j => j.ArticleId == articleId)
            .ToListAsync();

        return movements.Any() ? movements.Sum(j => j.Quantity) : 0m;
    }
}
