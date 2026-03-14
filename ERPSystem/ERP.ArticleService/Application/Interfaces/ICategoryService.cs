using ERP.ArticleService.Application.DTOs;
using ERP.ArticleService.Domain;

namespace ERP.ArticleService.Application.Interfaces
{
    public interface ICategoryService
    {
        // =========================
        // CREATE
        // =========================
        Task<Category> CreateAsync(string name, decimal tva);

        // =========================
        // READ
        // =========================
        Task<Category> GetByIdAsync(Guid id);
        Task<Category> GetByNameAsync(string name);
        Task<List<Category>> GetAllAsync();

        // =========================
        // TVA FILTERING
        // =========================
        Task<List<Category>> GetBelowTVAAsync(decimal tva);
        Task<List<Category>> GetHigherThanTVAAsync(decimal tva);
        Task<List<Category>> GetBetweenTVAAsync(decimal min, decimal max);

        // =========================
        // UPDATE
        // =========================
        Task<Category> UpdateAsync(Guid id, string newName, decimal tva);

        // =========================
        // DELETE
        // =========================
        Task DeleteAsync(Guid id);

        // =========================
        // PAGING / FILTERING
        // =========================
        Task<PagedResultDto<Category>> GetPagedAsync(
            int pageNumber,
            int pageSize);

        Task<PagedResultDto<Category>> GetPagedByNameAsync(
            string nameFilter,
            int pageNumber,
            int pageSize);

        Task<PagedResultDto<Category>> GetPagedByDateRangeAsync(
            DateTime from,
            DateTime to,
            int pageNumber,
            int pageSize);
    }
}