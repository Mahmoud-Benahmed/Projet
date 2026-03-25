using ERP.ArticleService.Application.DTOs;
using ERP.ArticleService.Domain;

namespace ERP.ArticleService.Application.Interfaces
{
    public interface IArticleService
    {
        // =========================
        // CREATE
        // =========================
        Task<ArticleResponseDto> CreateAsync(CreateArticleRequestDto request);

        // =========================
        // READ
        // =========================
        Task<ArticleResponseDto> GetByIdAsync(Guid id);
        Task<ArticleResponseDto> GetByCodeAsync(string code);

        // =========================
        // UPDATE
        // =========================
        Task<ArticleResponseDto> UpdateAsync(Guid id, UpdateArticleRequestDto request);

        // =========================
        // ACTIVATE / DEACTIVATE
        // =========================
        Task RestoreAsync(Guid id);

        // =========================
        // DELETE
        // =========================
        Task DeleteAsync(Guid id);

        // =========================
        // PAGING / FILTERING
        // =========================
        Task<PagedResultDto<ArticleResponseDto>> GetPagedByCategoryIdAsync(
            Guid categoryId,
            int pageNumber,
            int pageSize);

        Task<PagedResultDto<ArticleResponseDto>> GetPagedDeletedAsync(
            int pageNumber,
            int pageSize);

        Task<PagedResultDto<ArticleResponseDto>> GetAllAsync(
            int pageNumber,
            int pageSize);

        Task<PagedResultDto<ArticleResponseDto>> GetPagedByLibelleAsync(
            string libelleFilter,
            int pageNumber,
            int pageSize);


        Task<ArticleStatsDto> GetStatsAsync();
    }
}