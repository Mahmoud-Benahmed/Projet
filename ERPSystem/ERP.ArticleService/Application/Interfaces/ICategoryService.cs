using ERP.ArticleService.Application.DTOs;
using ERP.ArticleService.Domain;

namespace ERP.ArticleService.Application.Interfaces
{
    public interface ICategoryService
    {
        // =========================
        // CREATE
        // =========================
        Task<CategoryResponseDto> CreateAsync(CategoryRequestDto dto);

        // =========================
        // READ
        // =========================
        Task<CategoryResponseDto> GetByIdAsync(Guid id);
        Task<CategoryResponseDto> GetByNameAsync(string name);
        Task<List<CategoryResponseDto>> GetAllAsync();

        // =========================
        // TVA FILTERING
        // =========================
        Task<List<CategoryResponseDto>> GetBelowTVAAsync(decimal tva);
        Task<List<CategoryResponseDto>> GetHigherThanTVAAsync(decimal tva);
        Task<List<CategoryResponseDto>> GetBetweenTVAAsync(decimal min, decimal max);

        // =========================
        // UPDATE
        // =========================
        Task<CategoryResponseDto> UpdateAsync(Guid id, CategoryRequestDto dto);

        // =========================
        // DELETE
        // =========================
        Task DeleteAsync(Guid id);

        // =========================
        // PAGING / FILTERING
        // =========================
        Task<PagedResultDto<CategoryResponseDto>> GetPagedAsync(
            int pageNumber,
            int pageSize);

        Task<PagedResultDto<CategoryResponseDto>> GetPagedByNameAsync(
            string nameFilter,
            int pageNumber,
            int pageSize);

        Task<PagedResultDto<CategoryResponseDto>> GetPagedByDateRangeAsync(
            DateTime from,
            DateTime to,
            int pageNumber,
            int pageSize);
    }
}