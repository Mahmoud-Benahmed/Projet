using ERP.ArticleService.Application.DTOs;
using ERP.ArticleService.Application.Exceptions;
using ERP.ArticleService.Application.Interfaces;
using ERP.ArticleService.Domain;

namespace ERP.ArticleService.Application.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;

        public CategoryService(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        // =========================
        // CREATE
        // =========================
        public async Task<Category> CreateAsync(string name, decimal tva)
        {
            var existing = await _categoryRepository.GetByNameAsync(name);
            if (existing is not null)
                throw new CategoryAlreadyExistsException(name);

            var category = new Category(name, tva);
            await _categoryRepository.AddAsync(category);
            await _categoryRepository.SaveChangesAsync();
            return category;
        }

        // =========================
        // READ
        // =========================
        public async Task<Category> GetByIdAsync(Guid id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category is null)
                throw new CategoryNotFoundException(id);
            return category;
        }

        public async Task<Category> GetByNameAsync(string name)
        {
            var category = await _categoryRepository.GetByNameAsync(name);
            if (category is null)
                throw new CategoryNotFoundException(name);
            return category;
        }

        public async Task<List<Category>> GetAllAsync()
        {
            return await _categoryRepository.GetAllAsync();
        }


        public async Task<List<Category>> GetBelowTVAAsync(decimal tva)
        {
            if (tva <= 0)
                throw new ArgumentException("TVA must be greater than zero.");

            return await _categoryRepository.GetBelowTVAAsync(tva);
        }

        public async Task<List<Category>> GetHigherThanTVAAsync(decimal tva)
        {
            if (tva <= 0)
                throw new ArgumentException("TVA must be greater than zero.");

            return await _categoryRepository.GetHigherThanTVAAsync(tva);
        }

        public async Task<List<Category>> GetBetweenTVAAsync(decimal min, decimal max)
        {
            if (min <= 0)
                throw new ArgumentException("Min TVA must be greater than zero.");
            if (max <= 0)
                throw new ArgumentException("Max TVA must be greater than zero.");
            if (min > max)
                throw new ArgumentException("'min' TVA must be less than or equal to 'max' TVA.");

            return await _categoryRepository.GetBetweenTVAAsync(min, max);
        }

        // =========================
        // UPDATE
        // =========================
        public async Task<Category> UpdateAsync(Guid id, string newName, decimal tva)
        {
            var category = await GetByIdAsync(id);

            var existing = await _categoryRepository.GetByNameAsync(newName);
            if (existing is not null && existing.Id != id)
                throw new CategoryAlreadyExistsException(newName);

            category.Update(newName, tva);
            await _categoryRepository.SaveChangesAsync();
            return category;
        }

        // =========================
        // DELETE
        // =========================
        public async Task DeleteAsync(Guid id)
        {
            var category = await GetByIdAsync(id);
            _categoryRepository.Remove(category);
            await _categoryRepository.SaveChangesAsync();
        }

        // =========================
        // PAGING / FILTERING
        // =========================
        public async Task<PagedResultDto<Category>> GetPagedAsync(
            int pageNumber,
            int pageSize)
        {
            ValidatePaging(pageNumber, pageSize);
            var (items, totalCount) = await _categoryRepository
                .GetPagedAsync(pageNumber, pageSize);
            return new PagedResultDto<Category>(items, totalCount, pageNumber, pageSize);
        }

        public async Task<PagedResultDto<Category>> GetPagedByNameAsync(
            string nameFilter,
            int pageNumber,
            int pageSize)
        {
            ValidatePaging(pageNumber, pageSize);
            if (string.IsNullOrWhiteSpace(nameFilter))
                throw new ArgumentException("Name filter cannot be empty.");

            var (items, totalCount) = await _categoryRepository
                .GetPagedByNameAsync(nameFilter, pageNumber, pageSize);
            return new PagedResultDto<Category>(items, totalCount, pageNumber, pageSize);
        }

        public async Task<PagedResultDto<Category>> GetPagedByDateRangeAsync(
            DateTime from,
            DateTime to,
            int pageNumber,
            int pageSize)
        {
            ValidatePaging(pageNumber, pageSize);
            if (from > to)
                throw new ArgumentException("'from' date must be earlier than or equal to 'to' date.");

            var (items, totalCount) = await _categoryRepository
                .GetPagedByDateRangeAsync(from, to, pageNumber, pageSize);
            return new PagedResultDto<Category>(items, totalCount, pageNumber, pageSize);
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