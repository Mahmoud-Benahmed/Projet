using ERP.ClientService.Application.DTOs;
using ERP.ClientService.Application.Interfaces;
using ERP.ClientService.Domain;
using Microsoft.EntityFrameworkCore;

namespace ERP.ClientService.Infrastructure.Persistence.Repositories;

// ── Client ────────────────────────────────────────────────────────────────────

public class ClientRepository : IClientRepository
{
    private readonly ClientDbContext _context;

    public ClientRepository(ClientDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Client client) =>
        await _context.Clients.AddAsync(client);

    public Task SaveChangesAsync() =>
        _context.SaveChangesAsync();

    // Query filter applies — returns non-deleted only
    public Task<Client?> GetByIdAsync(Guid id) =>
        _context.Clients
                .Include(c => c.ClientCategories)
                    .ThenInclude(cc => cc.Category)
                .FirstOrDefaultAsync(c => c.Id == id);

    // IgnoreQueryFilters — intentionally fetches deleted records for Restore
    public Task<Client?> GetByIdDeletedAsync(Guid id) =>
        _context.Clients
                .IgnoreQueryFilters()
                .Include(c => c.ClientCategories)
                    .ThenInclude(cc => cc.Category)
                .FirstOrDefaultAsync(c => c.Id == id);

    // Query filter applies — only searches non-deleted clients
    public Task<Client?> GetByEmailAsync(string email) =>
        _context.Clients
                .FirstOrDefaultAsync(c =>
                    c.Email == email.Trim().ToLowerInvariant());

    public async Task<(List<Client> Items, int TotalCount)> GetAllAsync(
        int pageNumber, int pageSize)
    {
        // Query filter automatically excludes deleted
        var query = _context.Clients.OrderBy(c => c.Name);

        var total = await query.CountAsync();
        var items = await query
            .Include(c => c.ClientCategories).ThenInclude(cc => cc.Category)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<(List<Client> Items, int TotalCount)> GetPagedByCategoryIdAsync(
        Guid categoryId, int pageNumber, int pageSize)
    {
        var query = _context.Clients
                            .Where(c => c.ClientCategories
                                .Any(cc => cc.CategoryId == categoryId))
                            .OrderBy(c => c.Name);

        var total = await query.CountAsync();
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    // IgnoreQueryFilters — intentionally fetches only deleted records
    public async Task<(List<Client> Items, int TotalCount)> GetPagedDeletedAsync(
        int pageNumber, int pageSize)
    {
        var query = _context.Clients
                            .IgnoreQueryFilters()
                            .Where(c => c.IsDeleted)
                            .OrderByDescending(c => c.UpdatedAt);

        var total = await query.CountAsync();
        var items = await query
            .Include(c => c.ClientCategories).ThenInclude(cc => cc.Category)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<(List<Client> Items, int TotalCount)> GetPagedByNameAsync(
        string nameFilter, int pageNumber, int pageSize)
    {
        var term = nameFilter.Trim().ToLower();
        var query = _context.Clients
                            .Where(c => c.Name.ToLower().Contains(term))
                            .OrderBy(c => c.Name);

        var total = await query.CountAsync();
        var items = await query
            .Include(c => c.ClientCategories).ThenInclude(cc => cc.Category)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<ClientStatsDto> GetStatsAsync()
    {
        // IgnoreQueryFilters to count ALL clients including deleted
        var active = await _context.Clients.CountAsync(c => !c.IsBlocked);
        var blocked = await _context.Clients.CountAsync(c => c.IsBlocked);
        var deleted = await _context.Clients.IgnoreQueryFilters()
                                            .CountAsync(c => c.IsDeleted);
        var total = await _context.Clients.IgnoreQueryFilters().CountAsync() -(blocked+deleted);

        var rawPerCategory = await _context.Categories
                            .Select(c => new
                            {
                                CategoryId = c.Id,
                                CategoryName = c.Name,
                                ClientCount = c.ClientCategories.Count()
                            })
                            .OrderByDescending(x => x.ClientCount)
                            .ToListAsync();

        var perCategory = rawPerCategory
            .Select(x => new CategoryClientCountDto(
                x.CategoryId,
                x.CategoryName,
                x.ClientCount))
            .ToList();

        return new ClientStatsDto(total, active, blocked, deleted, perCategory);
    }
}