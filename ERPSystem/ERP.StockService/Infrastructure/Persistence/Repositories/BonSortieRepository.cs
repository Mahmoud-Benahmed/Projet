using ERP.StockService.Application.DTOs;
using ERP.StockService.Application.Interfaces;
using ERP.StockService.Domain;
using Microsoft.EntityFrameworkCore;

namespace ERP.StockService.Infrastructure.Persistence.Repositories;

public class BonSortieRepository : IBonSortieRepository
{
    private readonly StockDbContext _context;

    public BonSortieRepository(StockDbContext context) => _context = context;

    // =========================
    // CREATE / SAVE
    // =========================
    public async Task AddAsync(BonSortie b) => await _context.BonSorties.AddAsync(b);
    public async Task SaveChangesAsync() => await _context.SaveChangesAsync();

    // =========================
    // READ
    // =========================
    public async Task<BonSortie?> GetByIdAsync(Guid id) =>
        await _context.BonSorties
            .Include(b => b.Lignes)
            .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);

    public async Task<BonSortie?> GetByIdDeletedAsync(Guid id) =>
        await _context.BonSorties.IgnoreQueryFilters()
            .Include(b => b.Lignes)
            .FirstOrDefaultAsync(b => b.Id == id && b.IsDeleted);

    public async Task<(List<BonSortie> Items, int TotalCount)> GetAllAsync(int page, int size)
    {
        var query = _context.BonSorties
            .Include(b => b.Lignes)
            .Where(b => !b.IsDeleted);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        return (items, total);
    }

    public async Task<(List<BonSortie> Items, int TotalCount)> GetPagedDeletedAsync(int page, int size)
    {
        var query = _context.BonSorties.IgnoreQueryFilters()
            .Include(b => b.Lignes)
            .Where(b => b.IsDeleted);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(b => b.UpdatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        return (items, total);
    }

    public async Task<(List<BonSortie> Items, int TotalCount)> GetPagedByClientAsync(Guid clientId, int page, int size)
    {
        var query = _context.BonSorties
            .Include(b => b.Lignes)
            .Where(b => b.ClientId == clientId && !b.IsDeleted);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        return (items, total);
    }

    public async Task<(List<BonSortie> Items, int TotalCount)> GetPagedByDateRangeAsync(DateTime from, DateTime to, int page, int size)
    {
        var query = _context.BonSorties
            .Include(b => b.Lignes)
            .Where(b => !b.IsDeleted && b.CreatedAt >= from && b.CreatedAt <= to);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        return (items, total);
    }

    public async Task<(List<BonSortie> Items, int TotalCount)> GetByClientAsync(Guid clientId, int page, int size)
    {
        var query = _context.BonSorties
            .Include(b => b.Lignes)
            .Where(b => b.ClientId == clientId && !b.IsDeleted);

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        return (items, total);
    }

    public async Task<BonStatsDto> GetStatsAsync()
    {
        var counts = await _context.BonEntres
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Deleted = g.Count(b => b.IsDeleted),
            })
            .FirstOrDefaultAsync();

        return new BonStatsDto(
            TotalCount: counts?.Total ?? 0,
            ActiveCount: (counts?.Total ?? 0) - (counts?.Deleted ?? 0),
            DeletedCount: counts?.Deleted ?? 0
        );
    }
}