using ERP.ClientService.Application.DTOs;
using ERP.ClientService.Domain;

namespace ERP.ClientService.Application.Interfaces;

public interface IClientService
{
    Task<ClientResponseDto> CreateAsync(CreateClientRequestDto request);
    Task<ClientResponseDto> UpdateAsync(Guid id, UpdateClientRequestDto request);
    Task DeleteAsync(Guid id);
    Task RestoreAsync(Guid id);
    Task<ClientResponseDto> BlockAsync(Guid id);
    Task<ClientResponseDto> UnblockAsync(Guid id);
    Task<ClientResponseDto> SetCreditLimitAsync(Guid id, decimal limit);
    Task<ClientResponseDto> RemoveCreditLimitAsync(Guid id);
    Task<ClientResponseDto> SetDelaiRetourAsync(Guid id, int days);
    Task<ClientResponseDto> ClearDelaiRetourAsync(Guid id);
    Task<ClientResponseDto> AddCategoryAsync(Guid clientId, Guid categoryId, Guid assignedById);
    Task<ClientResponseDto> RemoveCategoryAsync(Guid clientId, Guid categoryId);
    Task<ClientResponseDto> GetByIdAsync(Guid id);
    Task<PagedResultDto<ClientResponseDto>> GetAllAsync(int pageNumber, int pageSize);
    Task<PagedResultDto<ClientResponseDto>> GetPagedDeletedAsync(int pageNumber, int pageSize);
    Task<PagedResultDto<ClientResponseDto>> GetPagedByCategoryIdAsync(Guid categoryId, int pageNumber, int pageSize);
    Task<PagedResultDto<ClientResponseDto>> GetPagedByNameAsync(string nameFilter, int pageNumber, int pageSize);
    Task<ClientStatsDto> GetStatsAsync();
    Task<int?> GetEffectiveDelaiRetourAsync(Guid id);
    Task<bool> CanPlaceOrderAsync(Guid id, decimal orderAmount, decimal currentBalance);
}