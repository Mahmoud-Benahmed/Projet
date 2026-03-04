using ERP.ArticleService.Application.DTOs;
using ERP.ArticleService.Domain;

namespace ERP.ArticleService.Application.Interfaces
{
    public interface IArticleService
    {
        // =========================
        // CREATE
        // =========================
        Task<Article> CreateAsync(string libelle, decimal prix, Guid categoryId);

        // =========================
        // READ
        // =========================
        Task<Article> GetByIdAsync(Guid id);
        Task<Article> GetByCodeAsync(string code);
        Task<List<Article>> GetAllAsync();

        // =========================
        // UPDATE
        // =========================
        Task<Article> UpdateAsync(Guid id, string libelle, decimal prix, Guid categoryId);

        // =========================
        // ACTIVATE / DEACTIVATE
        // =========================
        Task ActivateAsync(Guid id);
        Task DeactivateAsync(Guid id);

        // =========================
        // DELETE
        // =========================
        Task DeleteAsync(Guid id);

        // =========================
        // PAGING / FILTERING
        // =========================
        Task<PagedResultDto<Article>> GetPagedByCategoryIdAsync(
            Guid categoryId,
            int pageNumber,
            int pageSize);

        Task<PagedResultDto<Article>> GetPagedByStatusAsync(
            bool isActive,
            int pageNumber,
            int pageSize);

        Task<PagedResultDto<Article>> GetPagedByLibelleAsync(
            string libelleFilter,
            int pageNumber,
            int pageSize);
    }
}