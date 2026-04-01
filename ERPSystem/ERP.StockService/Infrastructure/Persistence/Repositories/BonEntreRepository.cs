using ERP.StockService.Application.DTOs;
using ERP.StockService.Domain;
using ERP.StockService.Domain.Entre;
using Microsoft.EntityFrameworkCore;

namespace ERP.StockService.Infrastructure.Persistence.Repositories
{
    public class BonEntreRepository : IBonEntreRepository
    {
        private readonly StockDbContext _context;

        public BonEntreRepository(StockDbContext context) => _context = context;

        public async Task AddAsync(BonEntre b) => await _context.BonEntres.AddAsync(b);
        public async Task SaveChangesAsync() => await _context.SaveChangesAsync();

        // ── base query helpers ────────────────────────────────────────────────
        // FIX: Include Fournisseur with IgnoreQueryFilters so that BonEntres
        // whose Fournisseur has been soft-deleted are still returned correctly.
        // The BonEntre-level query filter (!IsDeleted) is still applied by EF
        // automatically; we only bypass it for the Fournisseur navigation.
        private IQueryable<BonEntre> ActiveQuery() =>
            _context.BonEntres
                .Include(b => b.Lignes)
                .Include(b => b.Fournisseur)   // joined via filtered context
                .AsSplitQuery();               // avoids cartesian explosion on Lignes

        // For Fournisseur navigation: if the Fournisseur itself may be soft-deleted
        // we need to load it without its own query filter applied.
        // Use a manual join instead of navigation include to bypass the filter.
        private IQueryable<BonEntre> ActiveQueryWithFournisseur() =>
            _context.BonEntres
                .Include(b => b.Lignes)
                // IgnoreQueryFilters on the full query bypasses BOTH BonEntre and
                // Fournisseur filters, so we add the BonEntre filter back manually.
                .IgnoreQueryFilters()
                .Where(b => !b.IsDeleted)
                .Include(b => b.Fournisseur)   // now loads even soft-deleted fournisseurs
                .AsSplitQuery();

        // ── single record ─────────────────────────────────────────────────────
        public async Task<BonEntre?> GetByIdAsync(Guid id) =>
            await ActiveQueryWithFournisseur()
                .FirstOrDefaultAsync(b => b.Id == id);

        public async Task<BonEntre?> GetByIdDeletedAsync(Guid id) =>
            await _context.BonEntres
                .IgnoreQueryFilters()
                .Include(b => b.Lignes)
                .Include(b => b.Fournisseur)
                .AsSplitQuery()
                .FirstOrDefaultAsync(b => b.Id == id && b.IsDeleted);

        // ── list queries ──────────────────────────────────────────────────────
        public async Task<(List<BonEntre> Items, int TotalCount)> GetAllAsync(int page, int size)
        {
            // FIX: materialise count and items from the same base query so they
            // are always consistent — same filters, same joins.
            var query = ActiveQueryWithFournisseur()
                .OrderByDescending(b => b.CreatedAt);

            var total = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();

            return (items, total);
        }

        public async Task<(List<BonEntre> Items, int TotalCount)> GetByFournisseurAsync(
            Guid fournisseurId, int page, int size)
        {
            var query = ActiveQueryWithFournisseur()
                .Where(b => b.FournisseurId == fournisseurId)
                .OrderByDescending(b => b.CreatedAt);

            var total = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();

            return (items, total);
        }

        public async Task<(List<BonEntre> Items, int TotalCount)> GetPagedDeletedAsync(
            int page, int size)
        {
            var query = _context.BonEntres
                .IgnoreQueryFilters()
                .Include(b => b.Lignes)
                .Include(b => b.Fournisseur)
                .AsSplitQuery()
                .Where(b => b.IsDeleted)
                .OrderByDescending(b => b.CreatedAt);

            var total = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();

            return (items, total);
        }

        public async Task<(List<BonEntre> Items, int TotalCount)> GetPagedByDateRangeAsync(
            DateTime from, DateTime to, int page, int size)
        {
            var query = ActiveQueryWithFournisseur()
                .Where(b => from<= b.CreatedAt && b.CreatedAt <= to)
                .OrderByDescending(b => b.CreatedAt);

            var total = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();

            return (items, total);
        }

        // ── stats ─────────────────────────────────────────────────────────────
        public async Task<BonStatsDto> GetStatsAsync()
        {
            var counts = await _context.BonEntres
                .IgnoreQueryFilters()
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Active = g.Count(b => !b.IsDeleted),
                    Deleted = g.Count(b => b.IsDeleted),
                })
                .FirstOrDefaultAsync();

            return new BonStatsDto(
                TotalCount: counts?.Total ?? 0,
                ActiveCount: counts?.Active ?? 0,
                DeletedCount: counts?.Deleted ?? 0
            );
        }
    }
}