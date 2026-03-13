using ERP.ClientService.Application.DTOs;
using ERP.ClientService.Domain;
namespace ERP.ClientService.Application.Interfaces
{
    public interface IClientService
    {
        Task<ClientDto> GetByIdAsync(Guid id);
        Task<ClientDto> CreateAsync(CreateClientRequestDto dto);
        Task<ClientDto> UpdateAsync(Guid id, UpdateClientRequestDto dto);
        Task DeleteAsync(Guid id);
        Task RestoreAsync(Guid id);
        Task<PagedResultDto<ClientDto>> GetAllAsync(int pageNumber, int pageSize);
        Task<PagedResultDto<ClientDto>> GetPagedByTypeAsync(ClientType type, int pageNumber, int pageSize);
        Task<PagedResultDto<ClientDto>> GetPagedDeletedAsync(int pageNumber, int pageSize);
        Task<ClientStatsDto> GetStatsAsync();

    }
}
