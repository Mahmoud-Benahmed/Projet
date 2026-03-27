using ERP.ClientService.Application.DTOs;
using ERP.ClientService.Application.Interfaces;
using ERP.ClientService.Domain;
using Microsoft.EntityFrameworkCore;

namespace ERP.ClientService.Infrastructure.Persistence.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly ClientDbContext _context;

    public CategoryRepository(ClientDbContext context) => _context = context;

    public async Task AddAsync(Category category) =>
        await _context.Categories.AddAsync(category);

    public Task SaveChangesAsync() =>
        _context.SaveChangesAsync();

    public Task<Category?> GetByIdAsync(Guid id) =>
        _context.Categories.Include(c => c.ClientCategories)
                .ThenInclude(cc => cc.Client)
                .FirstOrDefaultAsync(c => c.Id == id);

    public Task<Category?> GetByIdDeletedAsync(Guid id) =>
        _context.Categories
                .IgnoreQueryFilters()
                .Include(c => c.ClientCategories)
                .ThenInclude(cc => cc.Client)
                .FirstOrDefaultAsync(c => c.Id == id);

    public Task<Category?> GetByCodeAsync(string code) =>
        _context.Categories
                .FirstOrDefaultAsync(c =>
                    c.Code == code.Trim().ToUpperInvariant());

    public async Task<List<Category>> GetAllAsync() =>
        await _context.Categories
                      .Where(c => c.IsActive)
                      .Include(c => c.ClientCategories)
                        .ThenInclude(cc => cc.Client)
                        .OrderBy(c => c.Name)
                        .ToListAsync();

    public async Task<(List<Category> Items, int TotalCount)> GetAllPagedAsync(
        int pageNumber, int pageSize)
    {
        var query = _context.Categories
                            .OrderBy(c => c.Name)
                            .Include(c => c.ClientCategories)
                            .ThenInclude(cc => cc.Client);

        var total = await query.CountAsync();
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<(List<Category> Items, int TotalCount)> GetPagedDeletedAsync(
        int pageNumber, int pageSize)
    {
        var query = _context.Categories
                            .IgnoreQueryFilters()
                            .Where(c => c.IsDeleted)
                            .Include(c => c.ClientCategories)
                            .ThenInclude(cc => cc.Client)
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
                            .Include(c => c.ClientCategories)
                            .ThenInclude(cc => cc.Client)
                            .OrderBy(c => c.Name);

        var total = await query.CountAsync();
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<List<Category>> GetAllActiveAsync() =>
        await _context.Categories
                      .Where(c => c.IsActive).Include(c => c.ClientCategories).ThenInclude(cc => cc.Client)
                      .OrderBy(c => c.Name)
                      .ToListAsync();

    public async Task<CategoryStatsDto> GetStatsAsync()
    {
        var counts = await _context.Categories
            .IgnoreQueryFilters()
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(c => !c.IsDeleted),
                Active = g.Count(c => c.IsActive && !c.IsDeleted),
                Inactive = g.Count(c => !c.IsActive && !c.IsDeleted),
                Deleted = g.Count(c => c.IsDeleted),
            })
            .FirstOrDefaultAsync();

        return counts is null
            ? new CategoryStatsDto(0, 0, 0, 0)
            : new CategoryStatsDto(counts.Total, counts.Active, counts.Inactive, counts.Deleted);
    }
}