using ERP.InvoiceService.Application.Interfaces;
using ERP.InvoiceService.Domain.LocalCache.Article;
using Microsoft.EntityFrameworkCore;

namespace ERP.InvoiceService.Infrastructure.Persistence.Repositories.LocalCache;

public sealed class ArticleCacheRepository : IArticleCacheRepository
{
    private readonly InvoiceDbContext _db;

    public ArticleCacheRepository(InvoiceDbContext db) => _db = db;

    public async Task<List<Domain.LocalCache.Article.ArticleCache>> GetByIdsAsync(List<Guid> ids) 
        => await _db.ArticleCaches
            .Include(a => a.Category)
            .Where(a => ids.Contains(a.Id))
            .ToListAsync();

    public async Task<Domain.LocalCache.Article.ArticleCache?> GetByIdAsync(Guid id)
    => await _db.ArticleCaches
        .Include(a => a.Category)
        .FirstOrDefaultAsync(a => a.Id == id);

    public async Task<Domain.LocalCache.Article.ArticleCache?> GetByBarCodeAsync(string barCode)
        => await _db.ArticleCaches
            .Include(a => a.Category)
            .FirstOrDefaultAsync(a => a.BarCode == barCode);

    public async Task<Domain.LocalCache.Article.ArticleCache?> GetByCodeRefAsync(string codeRef)
        => await _db.ArticleCaches
            .Include(a => a.Category)
            .FirstOrDefaultAsync(a => a.CodeRef == codeRef);

    public async Task<List<Domain.LocalCache.Article.ArticleCache>> GetAllAsync()
        => await _db.ArticleCaches
            .Include(a => a.Category)
            .OrderBy(a => a.Libelle)
            .ToListAsync();

    public async Task<List<Domain.LocalCache.Article.ArticleCache>> GetAllActiveAsync()
        => await _db.ArticleCaches
            .Include(a => a.Category)
            .OrderBy(a => a.Libelle)
            .ToListAsync();

    public async Task<(IEnumerable<Domain.LocalCache.Article.ArticleCache> Items, int TotalCount)> GetPagedAsync(
        int pageNumber, int pageSize)
    {
        var query = _db.ArticleCaches
            .Include(a => a.Category)
            .OrderBy(a => a.Libelle);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task AddAsync(Domain.LocalCache.Article.ArticleCache article)
        => await _db.ArticleCaches.AddAsync(article);

    public async Task SaveChangesAsync()
        => await _db.SaveChangesAsync();

    public async Task DeleteAsync(Domain.LocalCache.Article.ArticleCache article)
    {
        if (article == null)
            throw new ArgumentNullException(nameof(article));

        _db.ArticleCaches.Remove(article); // Use correct DbSet
        await _db.SaveChangesAsync();
    }
}