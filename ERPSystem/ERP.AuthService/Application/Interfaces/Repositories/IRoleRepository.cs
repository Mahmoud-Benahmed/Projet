using ERP.AuthService.Domain;

namespace ERP.AuthService.Application.Interfaces.Repositories
{
    public interface IRoleRepository
    {
        Task<Role?> GetByIdAsync(Guid id);
        Task<Role?> GetByLibelleAsync(RoleEnum libelle);
        Task<List<Role>> GetAllAsync();
        Task AddAsync(Role role);
        Task UpdateAsync(Role role);
        Task DeleteAsync(Guid id);
        Task<long> CountAsync();
    }
}
