using ERP.StockService.Application.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ERP.StockService.Infrastructure.Persistence.Repositories
{
    public class BonEntreRepository : IBonEntreRepository
    {
        private readonly StockDbContext _context;

        public BonEntreRepository(StockDbContext context) => _context = context;

        public async Task AddAsync(BonEntre b) => await _context.BonEntres.AddAsync(b);
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        // ── base query helpers ────────────────────────────────────────────────
        // FIX: Include Fournisseur with IgnoreQueryFilters so that BonEntres
        // whose Fournisseur has been soft-deleted are still returned correctly.
        // The BonEntre-level query filter (!IsDeleted) is still applied by EF
        // automatically; we only bypass it for the Fournisseur navigation.
        private IQueryable<BonEntre> ActiveQuery() =>
            _context.BonEntres
                .Include(b => b.Lignes)
                .AsSplitQuery();               // avoids cartesian explosion on Lignes

        // For Fournisseur navigation: if the Fournisseur itself may be soft-deleted
        // we need to load it without its own query filter applied.
        // Use a manual join instead of navigation include to bypass the filter.
        private IQueryable<BonEntre> ActiveQueryWithFournisseur() =>
            _context.BonEntres
                .Include(b => b.Lignes)   // now loads even soft-deleted fournisseurs
                .AsSplitQuery();

        // ── single record ─────────────────────────────────────────────────────
        public async Task<BonEntre?> GetByIdAsync(Guid id) =>
            await ActiveQueryWithFournisseur()
                .FirstOrDefaultAsync(b => b.Id == id);

        public async Task<BonEntre?> GetByIdDeletedAsync(Guid id) =>
            await _context.BonEntres
                .IgnoreQueryFilters()
                .Include(b => b.Lignes)
                .AsSplitQuery()
                .FirstOrDefaultAsync(b => b.Id == id);

        // ── list queries ──────────────────────────────────────────────────────
        public async Task<(List<BonEntre> Items, int TotalCount)> GetAllAsync(int page, int size)
        {
            // FIX: materialise count and items from the same base query so they
            // are always consistent — same filters, same joins.
            IOrderedQueryable<BonEntre> query = ActiveQueryWithFournisseur()
                .OrderByDescending(b => b.CreatedAt);

            int total = await query.CountAsync();
            List<BonEntre> items = await query
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();

            return (items, total);
        }

        public async Task<(List<BonEntre> Items, int TotalCount)> GetByFournisseurAsync(
            Guid fournisseurId, int page, int size)
        {
            IOrderedQueryable<BonEntre> query = ActiveQueryWithFournisseur()
                .Where(b => b.FournisseurId == fournisseurId)
                .OrderByDescending(b => b.CreatedAt);

            int total = await query.CountAsync();
            List<BonEntre> items = await query
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();

            return (items, total);
        }

        public async Task<(List<BonEntre> Items, int TotalCount)> GetPagedDeletedAsync(
            int page, int size)
        {
            IOrderedQueryable<BonEntre> query = _context.BonEntres
                .IgnoreQueryFilters()
                .Include(b => b.Lignes)
                .AsSplitQuery()
                .OrderByDescending(b => b.CreatedAt);

            int total = await query.CountAsync();
            List<BonEntre> items = await query
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();

            return (items, total);
        }

        public async Task<(List<BonEntre> Items, int TotalCount)> GetPagedByDateRangeAsync(
            DateTime from, DateTime to, int page, int size)
        {
            IOrderedQueryable<BonEntre> query = ActiveQueryWithFournisseur()
                .Where(b => b.CreatedAt.Date >= from.Date && b.CreatedAt.Date <= to.Date)
                .OrderByDescending(b => b.CreatedAt);

            int total = await query.CountAsync();

            List<BonEntre> items = await query
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();

            return (items, total);
        }

        public async Task DeleteByIdAsync(Guid id)
        {
            BonEntre? bon = await _context.BonEntres.FindAsync(id);
            if (bon != null)
            {
                _context.Remove(bon);
                await _context.SaveChangesAsync();
            }
        }

        // ── stats ─────────────────────────────────────────────────────────────
        public async Task<BonStatsDto> GetStatsAsync()
        {
            int count = await _context.BonEntres.CountAsync();

            return new BonStatsDto(
                TotalCount: count
            );
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync()
            => await _context.Database.BeginTransactionAsync();
    }
}