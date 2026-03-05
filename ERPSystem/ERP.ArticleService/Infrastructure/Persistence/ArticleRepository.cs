using ERP.ArticleService.Application.Interfaces;
using ERP.ArticleService.Domain;

using Microsoft.EntityFrameworkCore;

namespace ERP.ArticleService.Infrastructure.Persistence
{
    public class ArticleRepository : IArticleRepository
    {
        private readonly ArticleDbContext _context;

        public ArticleRepository(ArticleDbContext context)
        {
            _context = context;
        }

        private IQueryable<Article> BaseQuery() =>
            _context.Articles.Include(a => a.Category);

        // =========================
        // CREATE
        // =========================
        public async Task AddAsync(Article article)
        {
            await _context.Articles.AddAsync(article);
        }

        // =========================
        // READ - BY ID
        // =========================
        public async Task<Article?> GetByIdAsync(Guid id)
        {
            return await BaseQuery()
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        // =========================
        // READ - BY CODE
        // =========================
        public async Task<Article?> GetByCodeAsync(string code)
        {
            return await BaseQuery()
                .FirstOrDefaultAsync(a => a.Code == code);
        }

        // =========================
        // READ - ALL
        // =========================
        public async Task<List<Article>> GetAllAsync()
        {
            return await BaseQuery().OrderBy(a=> a.CreatedAt).ToListAsync();
        }

        // =========================
        // DELETE
        // =========================
        public void Remove(Article article)
        {
            _context.Articles.Remove(article);
        }

        // =========================
        // SAVE CHANGES
        // =========================
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        // =========================
        // PAGING / FILTERING
        // =========================

        public async Task<(List<Article> Items, int TotalCount)>
            GetPagedByCategoryIdAsync(Guid categoryId, int pageNumber, int pageSize)
        {
            var query = BaseQuery().Where(a => a.CategoryId == categoryId);

            return await PaginationHelper.ToPagedResultAsync(
                query, pageNumber, pageSize, q => q.OrderBy(a => a.CreatedAt));
        }


        public async Task<(List<Article> Items, int TotalCount)>
            GetPagedByStatusAsync(bool isActive, int pageNumber, int pageSize)
        {
            var query = BaseQuery()
                .Where(a => a.IsActive == isActive);

            return await PaginationHelper.ToPagedResultAsync(
                query, pageNumber, pageSize, q => q.OrderBy(a => a.CreatedAt));
        }

        public async Task<(List<Article> Items, int TotalCount)>
            GetPagedByLibelleAsync(string libelleFilter, int pageNumber, int pageSize)
        {
            var query = BaseQuery()
                .Where(a => EF.Functions.Like(a.Libelle, $"%{libelleFilter.Trim()}%"));

            return await PaginationHelper.ToPagedResultAsync(
                query, pageNumber, pageSize, q => q.OrderBy(a => a.Libelle));
        }
    }
}