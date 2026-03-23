using ERP.AuthService.Domain;
using MongoDB.Driver;

namespace ERP.AuthService.Application.Interfaces.Repositories
{
    public interface IRoleRepository
    {
        Task<Role?> GetByIdAsync(Guid id);
        Task<Role?> GetByLibelleAsync(RoleEnum libelle);
        Task<(List<Role> Items, int TotalCount)> GetAllAsync(int pageNumber, int pageSize); 
        Task<List<Role>> GetAllUnpagedAsync();
        Task AddAsync(Role role);
        Task UpdateAsync(Role role);
        Task DeleteAsync(Guid id);
        Task DeleteAllAsync();
        Task<long> CountAsync();
    }
}
