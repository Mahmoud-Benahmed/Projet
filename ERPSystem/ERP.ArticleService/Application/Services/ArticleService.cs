using ERP.ArticleService.Application.DTOs;
using ERP.ArticleService.Application.Exceptions;
using ERP.ArticleService.Application.Interfaces;
using ERP.ArticleService.Domain;

namespace ERP.ArticleService.Application.Services
{
    public class ArticleService : IArticleService
    {
        private readonly IArticleRepository _articleRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IArticleCodeService _articleCodeService;

        public ArticleService(
            IArticleRepository articleRepository,
            ICategoryRepository categoryRepository,
            IArticleCodeService articleCodeService)
        {
            _articleRepository = articleRepository;
            _categoryRepository = categoryRepository;
            _articleCodeService = articleCodeService;
        }

        // =========================
        // CREATE
        // =========================
        public async Task<Article> CreateAsync(CreateArticleRequestDto request)
        {
            var category = await _categoryRepository.GetByIdAsync(request.CategoryId)
                ?? throw new KeyNotFoundException(
                    $"Category with id '{request.CategoryId}' was not found.");

            var existing = await _articleRepository.GetByBarCodeAsync(request.BarCode);
            if (existing is not null)
                throw new ArticleAlreadyExistsException(existing.BarCode);

            var code = await _articleCodeService.GenerateArticleCodeAsync();

            var article = new Article(code, request.Libelle, request.Prix, category, request.BarCode, request.TVA);
            await _articleRepository.AddAsync(article);
            await _articleRepository.SaveChangesAsync();
            return article;
        }

        // =========================
        // READ
        // =========================
        public async Task<Article> GetByIdAsync(Guid id)
        {
            var article = await _articleRepository.GetByIdAsync(id);
            if (article is null || article.IsDeleted)
                throw new ArticleNotFoundException(id);
            return article;
        }

        public async Task<Article> GetByCodeAsync(string code)
        {
            var article = await _articleRepository.GetByCodeAsync(code);
            if (article is null || article.IsDeleted)
                throw new ArticleNotFoundException(code);
            return article;
        }

        // =========================
        // UPDATE
        // =========================
        public async Task<Article> UpdateAsync(Guid id, UpdateArticleRequestDto request)
        {
            var article = await _articleRepository.GetByIdAsync(id);

            if(article.IsDeleted || article is null)
                throw new ArticleNotFoundException(id);

            var category = await _categoryRepository.GetByIdAsync(request.CategoryId)
                ?? throw new CategoryNotFoundException(request.CategoryId);

            article.Update(request.Libelle, request.Prix, category, request.BarCode, request.TVA);

            await _articleRepository.SaveChangesAsync();
            return article;
        }

        // =========================
        // RESTORE
        // =========================
        public async Task RestoreAsync(Guid id)
        {
            var article = await GetByIdAsync(id);
            if (!article.IsDeleted)
                return;

            article.Restore();
            await _articleRepository.SaveChangesAsync();
        }


        // =========================
        // RESTORE
        // =========================
        public async Task DeleteAsync(Guid id)
        {
            var article = await GetByIdAsync(id);
            if (article.IsDeleted)
                return;

            article.Delete();
            await _articleRepository.SaveChangesAsync();
        }

        // =========================
        // PAGING / FILTERING
        // =========================
        public async Task<PagedResultDto<Article>> GetAllAsync(int pageNumber, int pageSize)
        {
            ValidatePaging(pageNumber, pageSize);

            var (items, totalCount) = await _articleRepository.GetAllAsync(pageNumber, pageSize);
            return new PagedResultDto<Article>(items, totalCount, pageNumber, pageSize);
        }
        
        public async Task<PagedResultDto<Article>> GetPagedByCategoryIdAsync(Guid categoryId, int pageNumber, int pageSize)
        {
            ValidatePaging(pageNumber, pageSize);
            var (items, totalCount) = await _articleRepository
                .GetPagedByCategoryIdAsync(categoryId, pageNumber, pageSize);
            return new PagedResultDto<Article>(items, totalCount, pageNumber, pageSize);
        }
        
        public async Task<PagedResultDto<Article>> GetPagedDeletedAsync(int pageNumber,int pageSize)
        {
            ValidatePaging(pageNumber, pageSize);
            var (items, totalCount) = await _articleRepository
                .GetPagedDeletedAsync(pageNumber, pageSize);
            return new PagedResultDto<Article>(items, totalCount, pageNumber, pageSize);
        }
        
        public async Task<PagedResultDto<Article>> GetPagedByLibelleAsync(string libelleFilter, int pageNumber,int pageSize)
        {
            ValidatePaging(pageNumber, pageSize);
            if (string.IsNullOrWhiteSpace(libelleFilter))
                throw new ArgumentException("Libelle filter cannot be empty.");

            var (items, totalCount) = await _articleRepository
                .GetPagedByLibelleAsync(libelleFilter, pageNumber, pageSize);
            return new PagedResultDto<Article>(items, totalCount, pageNumber, pageSize);
        }


        // ======================
        // STATS
        // ======================
        public async Task<ArticleStatsDto> GetStatsAsync()
        {
            return await _articleRepository.GetStatsAsync();
        }

        // =========================
        // PRIVATE HELPERS
        // =========================
        private static void ValidatePaging(int pageNumber, int pageSize)
        {
            if (pageNumber < 1)
                throw new ArgumentOutOfRangeException(nameof(pageNumber),
                    "Page number must be greater than zero.");
            if (pageSize < 1)
                throw new ArgumentOutOfRangeException(nameof(pageSize),
                    "Page size must be greater than zero.");
        }
    }
}