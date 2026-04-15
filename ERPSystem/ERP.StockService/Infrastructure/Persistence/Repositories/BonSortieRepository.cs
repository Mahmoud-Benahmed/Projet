using ERP.StockService.Application.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ERP.StockService.Infrastructure.Persistence.Repositories;

public class BonSortieRepository : IBonSortieRepository
{
    private readonly StockDbContext _context;

    public BonSortieRepository(StockDbContext context) => _context = context;

    // =========================
    // CREATE / SAVE
    // =========================
    public async Task AddAsync(BonSortie b) => await _context.BonSorties.AddAsync(b);
    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task DeleteByIdAsync(Guid id)
    {
        var bon = await _context.BonSorties.FindAsync(id);
        if (bon != null)
        {
            _context.Remove(bon);
            await _context.SaveChangesAsync();
        }
    }

    // =========================
    // READ
    // =========================
    public async Task<BonSortie?> GetByIdAsync(Guid id) =>
        await _context.BonSorties
            .Include(b => b.Lignes)
            .FirstOrDefaultAsync(b => b.Id == id);

    public async Task<BonSortie?> GetByIdDeletedAsync(Guid id) =>
        await _context.BonSorties
            .Include(b => b.Lignes)
            .FirstOrDefaultAsync(b => b.Id == id);

    public async Task<(List<BonSortie> Items, int TotalCount)> GetAllAsync(int page, int size)
    {
        var query = _context.BonSorties
            .Include(b => b.Lignes);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        return (items, total);
    }

    public async Task<(List<BonSortie> Items, int TotalCount)> GetPagedByClientAsync(Guid clientId, int page, int size)
    {
        var query = _context.BonSorties
            .Include(b => b.Lignes)
            .Where(b => b.ClientId == clientId);

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
            .Where(b => b.CreatedAt.Date >= from.Date && b.CreatedAt.Date <= to.Date);

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
            .Where(b => b.ClientId == clientId);

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
        var count = await _context.BonSorties.CountAsync();

        return new BonStatsDto(
            TotalCount: count
        );
    }
    public async Task<IDbContextTransaction> BeginTransactionAsync()
            => await _context.Database.BeginTransactionAsync();
}