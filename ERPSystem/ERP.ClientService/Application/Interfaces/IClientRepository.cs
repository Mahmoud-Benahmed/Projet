using ERP.ClientService.Application.DTOs;
using ERP.ClientService.Domain;

namespace ERP.ClientService.Application.Interfaces
{
    public interface IClientRepository
    {
        Task AddAsync(Client client);
        Task<Client?> GetByIdAsync(Guid id);
        Task<Client?> GetByIdDeletedAsync(Guid id);

        Task SaveChangesAsync();

        // Paging & filtering
        Task<(List<Client> Items, int TotalCount)> GetAllAsync(int pageNumber, int pageSize);
        Task<(List<Client> Items, int TotalCount)> GetPagedByTypeAsync(ClientType type, int pageNumber, int pageSize);
        Task<(List<Client> Items, int TotalCount)> GetPagedDeletedAsync(int pageNumber, int pageSize);

        Task<ClientStatsDto> GetStatsAsync();
    }
}