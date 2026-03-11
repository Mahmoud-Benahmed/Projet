using ERP.ArticleService.Application.DTOs;
using ERP.ArticleService.Application.Interfaces;
using ERP.ArticleService.Domain;

using Microsoft.EntityFrameworkCore;
using System.Net.Quic;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

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
                .FirstOrDefaultAsync(a => a.CodeRef == code);
        }

        // =========================
        // READ - BY CODE
        // =========================

        public async Task<Article?> GetByBarCodeAsync(string barCode)
        {
            return await BaseQuery()
                .FirstOrDefaultAsync(a => a.BarCode== barCode);
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
            var query = BaseQuery().Where(a => !a.IsDeleted);
            return await PaginationHelper.ToPagedResultAsync(
                    query, pageNumber, pageSize, q => q.OrderBy(a => a.CreatedAt));
        }

        public async Task<(List<Article> Items, int TotalCount)> GetPagedByCategoryIdAsync(Guid categoryId, int pageNumber, int pageSize)
        {
            var query = BaseQuery().Where(a => a.CategoryId == categoryId && !a.IsDeleted);
            return await PaginationHelper.ToPagedResultAsync(
                query, pageNumber, pageSize, q => q.OrderBy(a => a.CreatedAt));
        }

        public async Task<(List<Article> Items, int TotalCount)> GetPagedByLibelleAsync(string libelleFilter, int pageNumber, int pageSize)
        {
            var query = BaseQuery().Where(a => EF.Functions.Like(a.Libelle, $"%{libelleFilter.Trim()}%") && !a.IsDeleted);
            return await PaginationHelper.ToPagedResultAsync(
                query, pageNumber, pageSize, q => q.OrderBy(a => a.Libelle));
        }

        public async Task<(List<Article> Items, int TotalCount)> GetPagedDeletedAsync(int pageNumber, int pageSize)
        {
            var query = BaseQuery().Where(a => a.IsDeleted);
            return await PaginationHelper.ToPagedResultAsync(
                query, pageNumber, pageSize, q => q.OrderBy(a => a.CreatedAt));
        }

        // ========================
        // STATS
        // ========================
        public async Task<ArticleStatsDto> GetStatsAsync()
        {
            var total = await _context.Articles.CountAsync();
            var active = await _context.Articles.CountAsync(a => !a.IsDeleted);
            var deleted = await _context.Articles.CountAsync(a => a.IsDeleted);
            var categoriesCount = await _context.Categories.CountAsync();

            return new ArticleStatsDto
            (
                TotalCount: total,
                ActiveCount : active,
                DeletedCount : deleted,
                CategoriesCount : categoriesCount
            );
        }
    }
}