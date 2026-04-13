using ERP.StockService.Application.Interfaces;
using ERP.StockService.Domain.LocalCache.Article;
using Microsoft.EntityFrameworkCore;

namespace ERP.StockService.Infrastructure.Persistence.Repositories.LocalCache.ArticleCache;

public sealed class CategoryCacheRepository : ICategoryCacheRepository
{
    private readonly StockDbContext _db;

    public CategoryCacheRepository(StockDbContext db) => _db = db;

    public async Task<CategoryCache?> GetByIdAsync(Guid id)
        => await _db.ArticleCategoryCaches.FindAsync(id);

    public async Task<CategoryCache?> GetByNameAsync(string name)
        => await _db.ArticleCategoryCaches
            .FirstOrDefaultAsync(c => c.Name == name);

    public async Task<List<CategoryCache>> GetAllAsync()
        => await _db.ArticleCategoryCaches
            .OrderBy(c => c.Name)
            .ToListAsync();

    public async Task<List<CategoryCache>> GetAllActiveAsync()
        => await _db.ArticleCategoryCaches
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.Name)
            .ToListAsync();

    public async Task<(List<CategoryCache> Items, int TotalCount)> GetPagedAsync(
        int pageNumber, int pageSize)
    {
        var query = _db.ArticleCategoryCaches
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.Name);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task DeleteAsync(CategoryCache category)
    {
        if (category == null)
            throw new ArgumentNullException(nameof(category));

        _db.ArticleCategoryCaches.Remove(category); // Use correct DbSet
        await _db.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(string name)
    {
        return await _db.ArticleCategoryCaches.AnyAsync(c => c.Name == name);
    }

    public async Task AddAsync(CategoryCache category)
        => await _db.ArticleCategoryCaches.AddAsync(category);
    public async Task SaveChangesAsync()
        => await _db.SaveChangesAsync();
}