using ERP.ClientService.Application.DTOs;
using ERP.ClientService.Application.Interfaces;
using ERP.ClientService.Domain;
using Microsoft.EntityFrameworkCore;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace ERP.ClientService.Infrastructure.Persistence.Repositories;
public class CategoryRepository : ICategoryRepository
{
    private readonly ClientDbContext _context;

    public CategoryRepository(ClientDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Category category) =>
        await _context.Categories.AddAsync(category);

    public Task SaveChangesAsync() =>
        _context.SaveChangesAsync();

    // Query filter applies — returns non-deleted only
    public Task<Category?> GetByIdAsync(Guid id) =>
        _context.Categories
                .Include(c => c.ClientCategories)
                .FirstOrDefaultAsync(c => c.Id == id);

    // IgnoreQueryFilters — intentionally fetches deleted records for Restore
    public Task<Category?> GetByIdDeletedAsync(Guid id) =>
        _context.Categories
                .IgnoreQueryFilters()
                .Include(c => c.ClientCategories)
                .FirstOrDefaultAsync(c => c.Id == id);

    // Query filter applies — only searches non-deleted categories
    public Task<Category?> GetByCodeAsync(string code) =>
        _context.Categories
                .FirstOrDefaultAsync(c =>
                    c.Code == code.Trim().ToUpperInvariant());

    public async Task<(List<Category> Items, int TotalCount)> GetAllAsync(
        int pageNumber, int pageSize)
    {
        var query = _context.Categories
                            .Include(c => c.ClientCategories)
                            .OrderBy(c => c.Name);

        var total = await query.CountAsync();
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    // IgnoreQueryFilters — intentionally fetches only deleted records
    public async Task<(List<Category> Items, int TotalCount)> GetPagedDeletedAsync(
        int pageNumber, int pageSize)
    {
        var query = _context.Categories
                            .IgnoreQueryFilters()
                            .Where(c => c.IsDeleted)
                            .OrderByDescending(c => c.UpdatedAt);

        var total = await query.CountAsync();
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<(List<Category> Items, int TotalCount)> GetPagedByNameAsync(
        string nameFilter, int pageNumber, int pageSize)
    {
        var term = nameFilter.Trim().ToLower();
        var query = _context.Categories
                            .Where(c => c.Name.ToLower().Contains(term))
                            .OrderBy(c => c.Name);

        var total = await query.CountAsync();
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    // No paging — full list for dropdowns, query filter excludes deleted automatically
    public async Task<List<Category>> GetAllActiveAsync() =>
        await _context.Categories
                      .Where(c => c.IsActive)
                      .OrderBy(c => c.Name)
                      .ToListAsync();

    public async Task<CategoryStatsDto> GetStatsAsync()
    {
        // IgnoreQueryFilters to count ALL categories including deleted
        var active = await _context.Categories.CountAsync(c => c.IsActive);
        var inactive = await _context.Categories.CountAsync(c => !c.IsActive);
        var deleted = await _context.Categories.IgnoreQueryFilters()
                                                .CountAsync(c => c.IsDeleted);
        var total = await _context.Categories.IgnoreQueryFilters().CountAsync() -deleted;


        return new CategoryStatsDto(total, active, inactive, deleted);
    }
}