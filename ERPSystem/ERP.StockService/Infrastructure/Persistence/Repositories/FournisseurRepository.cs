using ERP.StockService.Application.DTOs;
using ERP.StockService.Domain;
using Microsoft.EntityFrameworkCore;

namespace ERP.StockService.Infrastructure.Persistence.Repositories;

public class FournisseurRepository : IFournisseurRepository
{
    private readonly StockDbContext _context;

    public FournisseurRepository(StockDbContext context) => _context = context;

    // =========================
    // CREATE / SAVE
    // =========================
    public async Task AddAsync(Fournisseur f) => await _context.Fournisseurs.AddAsync(f);
    public async Task SaveChangesAsync() => await _context.SaveChangesAsync();

    // =========================
    // READ BY ID
    // =========================
    public async Task<Fournisseur?> GetByIdAsync(Guid id) =>
        await _context.Fournisseurs.FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);

    public async Task<Fournisseur?> GetByIdDeletedAsync(Guid id) =>
        await _context.Fournisseurs.FirstOrDefaultAsync(f => f.Id == id && f.IsDeleted);

    // =========================
    // PAGING
    // =========================
    public async Task<(List<Fournisseur> Items, int TotalCount)> GetAllAsync(int page, int size)
    {
        ValidatePaging(page, size);

        var query = _context.Fournisseurs.Where(f => !f.IsDeleted);

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(f => f.Name)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        return (items, total);
    }

    public async Task<(List<Fournisseur> Items, int TotalCount)> GetPagedDeletedAsync(int page, int size)
    {
        ValidatePaging(page, size);

        var query = _context.Fournisseurs.Where(f => f.IsDeleted);

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(f => f.Name)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        return (items, total);
    }

    public async Task<(List<Fournisseur> Items, int TotalCount)> GetPagedByNameAsync(
    string nameFilter, int page, int size)
    {
        var query = _context.Fournisseurs
            .Where(f => !f.IsDeleted && f.Name.Contains(nameFilter.Trim()));

        var total = await query.CountAsync();

        var items = await query
            .OrderBy(f => f.Name)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        return (items, total);
    }

    // =========================
    // STATS
    // =========================
    public async Task<StockStatsDto> GetStatsAsync()
    {
        // fournisseurs
        var totalFournisseurs = await _context.Fournisseurs.CountAsync();
        var activeFournisseurs = await _context.Fournisseurs.CountAsync(f => !f.IsDeleted && !f.IsBlocked);
        var blockedFournisseurs = await _context.Fournisseurs.CountAsync(f => f.IsBlocked && !f.IsDeleted);

        // bon entre, sortie, retours
        var totalBonEntres = await _context.BonEntres.CountAsync(b => !b.IsDeleted);
        var totalBonSorties = await _context.BonSorties.CountAsync(b => !b.IsDeleted);
        var totalBonRetours = await _context.BonRetours.CountAsync(b => !b.IsDeleted);

        return new StockStatsDto(
            TotalFournisseurs: totalFournisseurs,
            ActiveFournisseurs: activeFournisseurs,
            BlockedFournisseurs: blockedFournisseurs,
            TotalBonEntres: totalBonEntres,
            TotalBonSorties: totalBonSorties,
            TotalBonRetours: totalBonRetours
        );
    }

    // =========================
    // HELPERS
    // =========================
    private static void ValidatePaging(int page, int size)
    {
        if (page < 1) throw new ArgumentOutOfRangeException(nameof(page), "Page number must be greater than zero.");
        if (size < 1) throw new ArgumentOutOfRangeException(nameof(size), "Page size must be greater than zero.");
    }
}