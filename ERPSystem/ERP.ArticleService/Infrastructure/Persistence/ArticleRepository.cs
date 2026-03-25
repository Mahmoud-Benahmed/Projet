using ERP.ArticleService.Application.DTOs;
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

        public async Task<Article?> GetByIdDeletedAsync(Guid id)
        {
            return await BaseQuery()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        // =========================
        // READ - BY CODE
        // =========================
        public async Task<Article?> GetByCodeAsync(string code)
        {
            return await BaseQuery()
                .FirstOrDefaultAsync(a => a.CodeRef == code || a.BarCode == code);
        }

        // =========================
        // READ - BY BARCODE
        // =========================
        public async Task<Article?> GetByBarCodeAsync(string barCode)
        {
            return await BaseQuery()
                .FirstOrDefaultAsync(a => a.BarCode == barCode);
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
        public async Task<(List<Article> Items, int TotalCount)> GetAllAsync(int pageNumber, int pageSize)
        {
            // HasQueryFilter handles !IsDeleted automatically
            var query = BaseQuery();
            return await PaginationHelper.ToPagedResultAsync(
                query, pageNumber, pageSize, q => q.OrderBy(a => a.CreatedAt));
        }

        public async Task<(List<Article> Items, int TotalCount)> GetPagedByCategoryIdAsync(Guid categoryId, int pageNumber, int pageSize)
        {
            // HasQueryFilter handles !IsDeleted automatically
            var query = BaseQuery().Where(a => a.CategoryId == categoryId);
            return await PaginationHelper.ToPagedResultAsync(
                query, pageNumber, pageSize, q => q.OrderBy(a => a.CreatedAt));
        }

        public async Task<(List<Article> Items, int TotalCount)> GetPagedByLibelleAsync(string libelleFilter, int pageNumber, int pageSize)
        {
            // HasQueryFilter handles !IsDeleted automatically
            var query = BaseQuery().Where(a => EF.Functions.Like(a.Libelle, $"%{libelleFilter.Trim()}%"));
            return await PaginationHelper.ToPagedResultAsync(
                query, pageNumber, pageSize, q => q.OrderBy(a => a.Libelle));
        }

        public async Task<(List<Article> Items, int TotalCount)> GetPagedDeletedAsync(int pageNumber, int pageSize)
        {
            // IgnoreQueryFilters to bypass HasQueryFilter, then filter deleted only
            var query = BaseQuery()
                .IgnoreQueryFilters()
                .Where(a => a.IsDeleted);
            return await PaginationHelper.ToPagedResultAsync(
                query, pageNumber, pageSize, q => q.OrderBy(a => a.CreatedAt));
        }

        // =========================
        // STATS
        // =========================
        public async Task<ArticleStatsDto> GetStatsAsync()
        {
            var total = await _context.Articles.IgnoreQueryFilters().CountAsync();
            var active = await _context.Articles.CountAsync();
            var deleted = await _context.Articles.IgnoreQueryFilters().CountAsync(a => a.IsDeleted);
            var categoriesCount = await _context.Categories.CountAsync();

            return new ArticleStatsDto(
                TotalCount: total,
                ActiveCount: active,
                DeletedCount: deleted,
                CategoriesCount: categoriesCount
            );
        }
    }
}