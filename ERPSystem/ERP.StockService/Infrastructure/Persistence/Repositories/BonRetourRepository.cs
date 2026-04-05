using ERP.StockService.Application.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ERP.StockService.Infrastructure.Persistence.Repositories;

public class BonRetourRepository : IBonRetourRepository
{
    private readonly StockDbContext _context;

    public BonRetourRepository(StockDbContext context) => _context = context;

    // =========================
    // CREATE
    // =========================
    public async Task AddAsync(BonRetour b) => await _context.BonRetours.AddAsync(b);
    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task DeleteByIdAsync(Guid id)
    {
        var bon = await _context.BonRetours.FindAsync(id);
        if (bon != null)
        {
            _context.Remove(bon);
            await _context.SaveChangesAsync();
        }
    }

    // =========================
    // READ
    // =========================
    public async Task<BonRetour?> GetByIdAsync(Guid id) =>
        await _context.BonRetours
            .Include(b => b.Lignes)
            .FirstOrDefaultAsync(b => b.Id == id);

    public async Task<(List<BonRetour> Items, int TotalCount)> GetAllAsync(int page, int size)
    {
        var query = _context.BonRetours
            .Include(b => b.Lignes);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        return (items, total);
    }

    public async Task<(List<BonRetour> Items, int TotalCount)> GetBySourceIdAsync(Guid sourceId, int page, int size)
    {
        var query = _context.BonRetours
            .Include(b => b.Lignes)
            .Where(b => b.SourceId == sourceId);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        return (items, total);
    }

    public async Task<(List<BonRetour> Items, int TotalCount)> GetByRetourSourceTypeAsync(RetourSourceType sourceType, int page, int size)
    {
        var query = _context.BonRetours
            .Include(b => b.Lignes)
            .Where(b => b.SourceType == sourceType);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        return (items, total);
    }

    public Task<(List<BonRetour> Items, int TotalCount)> GetPagedBySourceAsync(Guid sourceId, int page, int size)
    {
        return GetBySourceIdAsync(sourceId, page, size);
    }

    public async Task<(List<BonRetour> Items, int TotalCount)> GetPagedByDateRangeAsync(DateTime from, DateTime to, int page, int size)
    {
        var query = _context.BonRetours
            .Include(b => b.Lignes)
            .Where(b => b.CreatedAt.Date >= from.Date && b.CreatedAt.Date <= to.Date);

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
        var count = await _context.BonRetours.CountAsync();

        return new BonStatsDto(
            TotalCount: count
        );
    }
}