using ERP.ArticleService.Application.DTOs;
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
        public async Task<Article> CreateAsync(string libelle, decimal prix, Guid categoryId)
        {
            var category = await _categoryRepository.GetByIdAsync(categoryId)
                ?? throw new KeyNotFoundException(
                    $"Category with id '{categoryId}' was not found.");

            var code = await _articleCodeService.GenerateArticleCodeAsync();

            var article = new Article(code, libelle, prix, category);
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
            if (article is null)
                throw new KeyNotFoundException(
                    $"Article with id '{id}' was not found.");
            return article;
        }

        public async Task<Article> GetByCodeAsync(string code)
        {
            var article = await _articleRepository.GetByCodeAsync(code);
            if (article is null)
                throw new KeyNotFoundException(
                    $"Article with code '{code}' was not found.");
            return article;
        }

        public async Task<List<Article>> GetAllAsync()
        {
            return await _articleRepository.GetAllAsync();
        }

        // =========================
        // UPDATE
        // =========================
        public async Task<Article> UpdateAsync(Guid id, string libelle, decimal prix, Guid categoryId)
        {
            var article = await GetByIdAsync(id);

            var category = await _categoryRepository.GetByIdAsync(categoryId)
                ?? throw new KeyNotFoundException(
                    $"Category with id '{categoryId}' was not found.");

            article.Update(libelle, prix, category);
            await _articleRepository.SaveChangesAsync();
            return article;
        }

        // =========================
        // ACTIVATE / DEACTIVATE
        // =========================
        public async Task ActivateAsync(Guid id)
        {
            var article = await GetByIdAsync(id);
            article.Activate();
            await _articleRepository.SaveChangesAsync();
        }

        public async Task DeactivateAsync(Guid id)
        {
            var article = await GetByIdAsync(id);
            article.Deactivate();
            await _articleRepository.SaveChangesAsync();
        }

        // =========================
        // DELETE
        // =========================
        public async Task DeleteAsync(Guid id)
        {
            var article = await GetByIdAsync(id);
            _articleRepository.Remove(article);
            await _articleRepository.SaveChangesAsync();
        }

        // =========================
        // PAGING / FILTERING
        // =========================
        public async Task<PagedResultDto<Article>> GetPagedByCategoryIdAsync(
            Guid categoryId,
            int pageNumber,
            int pageSize)
        {
            ValidatePaging(pageNumber, pageSize);
            var (items, totalCount) = await _articleRepository
                .GetPagedByCategoryIdAsync(categoryId, pageNumber, pageSize);
            return new PagedResultDto<Article>(items, totalCount, pageNumber, pageSize);
        }

        public async Task<PagedResultDto<Article>> GetPagedByStatusAsync(
            bool isActive,
            int pageNumber,
            int pageSize)
        {
            ValidatePaging(pageNumber, pageSize);
            var (items, totalCount) = await _articleRepository
                .GetPagedByStatusAsync(isActive, pageNumber, pageSize);
            return new PagedResultDto<Article>(items, totalCount, pageNumber, pageSize);
        }

        public async Task<PagedResultDto<Article>> GetPagedByLibelleAsync(
            string libelleFilter,
            int pageNumber,
            int pageSize)
        {
            ValidatePaging(pageNumber, pageSize);
            if (string.IsNullOrWhiteSpace(libelleFilter))
                throw new ArgumentException("Libelle filter cannot be empty.");

            var (items, totalCount) = await _articleRepository
                .GetPagedByLibelleAsync(libelleFilter, pageNumber, pageSize);
            return new PagedResultDto<Article>(items, totalCount, pageNumber, pageSize);
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