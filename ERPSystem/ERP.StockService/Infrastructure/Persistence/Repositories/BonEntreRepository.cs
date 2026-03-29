using Microsoft.EntityFrameworkCore;
using ERP.StockService.Domain;
using ERP.StockService.Domain.Entre;

namespace ERP.StockService.Infrastructure.Persistence.Repositories
{
    public class BonEntreRepository : IBonEntreRepository
    {
        private readonly StockDbContext _context;

        public BonEntreRepository(StockDbContext context) => _context = context;

        public async Task AddAsync(BonEntre b) => await _context.BonEntres.AddAsync(b);
        public async Task SaveChangesAsync() => await _context.SaveChangesAsync();

        // Include Lignes and Fournisseur
        public async Task<BonEntre?> GetByIdAsync(Guid id) =>
            await _context.BonEntres
                .Include(b => b.Lignes)
                .Include(b => b.Fournisseur)
                .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);

        public async Task<BonEntre?> GetByIdDeletedAsync(Guid id) =>
            await _context.BonEntres
                .Include(b => b.Lignes)
                .Include(b => b.Fournisseur)
                .FirstOrDefaultAsync(b => b.Id == id && b.IsDeleted);

        public async Task<(List<BonEntre> Items, int TotalCount)> GetAllAsync(int page, int size)
        {
            var query = _context.BonEntres
                .Include(b => b.Lignes)
                .Include(b => b.Fournisseur)
                .Where(b => !b.IsDeleted)
                .OrderByDescending(b => b.CreatedAt);

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * size).Take(size).ToListAsync();
            return (items, total);
        }

        public async Task<(List<BonEntre> Items, int TotalCount)> GetByFournisseurAsync(Guid fournisseurId, int page, int size)
        {
            var query = _context.BonEntres
                .Include(b => b.Lignes)
                .Include(b => b.Fournisseur)
                .Where(b => !b.IsDeleted && b.FournisseurId == fournisseurId)
                .OrderByDescending(b => b.CreatedAt);

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * size).Take(size).ToListAsync();
            return (items, total);
        }

        public async Task<(List<BonEntre> Items, int TotalCount)> GetPagedDeletedAsync(int page, int size)
        {
            var query = _context.BonEntres
                .Include(b => b.Lignes)
                .Include(b => b.Fournisseur)
                .Where(b => b.IsDeleted)
                .OrderByDescending(b => b.CreatedAt);

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * size).Take(size).ToListAsync();
            return (items, total);
        }

        // Optional: filter by date range
        public async Task<(List<BonEntre> Items, int TotalCount)> GetPagedByDateRangeAsync(DateTime from, DateTime to, int page, int size)
        {
            var query = _context.BonEntres
                .Include(b => b.Lignes)
                .Include(b => b.Fournisseur)
                .Where(b => !b.IsDeleted && b.CreatedAt >= from && b.CreatedAt <= to)
                .OrderByDescending(b => b.CreatedAt);

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * size).Take(size).ToListAsync();
            return (items, total);
        }
    }
}