using ERP.ClientService.Application.DTOs;

namespace ERP.ClientService.Application.Interfaces;

public interface ICategoryService
{
    Task<CategoryResponseDto> CreateAsync(CreateCategoryRequestDto request);
    Task<CategoryResponseDto> UpdateAsync(Guid id, UpdateCategoryRequestDto request);
    Task DeleteAsync(Guid id);
    Task RestoreAsync(Guid id);
    Task<CategoryResponseDto> ActivateAsync(Guid id);
    Task<CategoryResponseDto> DeactivateAsync(Guid id);
    Task<CategoryResponseDto> GetByIdAsync(Guid id);
    Task<PagedResultDto<CategoryResponseDto>> GetAllAsync(int pageNumber, int pageSize);
    Task<PagedResultDto<CategoryResponseDto>> GetPagedDeletedAsync(int pageNumber, int pageSize);
    Task<PagedResultDto<CategoryResponseDto>> GetPagedByNameAsync(string nameFilter, int pageNumber, int pageSize);
    Task<CategoryStatsDto> GetStatsAsync();
}
