using ERP.ArticleService.Application.DTOs;
using ERP.ArticleService.Domain;

namespace ERP.ArticleService.Application.Interfaces
{
    public interface ICategoryService
    {
        // =========================
        // CREATE
        // =========================
        Task<Category> CreateAsync(string name);

        // =========================
        // READ
        // =========================
        Task<Category> GetByIdAsync(Guid id);
        Task<Category> GetByNameAsync(string name);
        Task<List<Category>> GetAllAsync();

        // =========================
        // UPDATE
        // =========================
        Task<Category> UpdateNameAsync(Guid id, string newName);

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