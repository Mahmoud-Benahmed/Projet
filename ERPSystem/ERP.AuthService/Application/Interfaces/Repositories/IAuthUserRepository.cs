using ERP.AuthService.Application.DTOs.AuthUser;
using ERP.AuthService.Domain;

namespace ERP.AuthService.Application.Interfaces.Repositories
{
    public interface IAuthUserRepository
    {
        Task AddAsync(AuthUser user);
        Task<(List<AuthUser>, int TotalCount)> GetAllAsync(int pageNumber, int pageSize);
        Task<AuthUser?> GetByLoginAsync(string login);
        Task<AuthUser?> GetByEmailAsync(string email);
        Task<AuthUser?> GetByIdAsync(Guid id);
        Task<(List<AuthUser>, int TotalCount)> GetPagedByStatusAsync(bool isActive, int pageNumber, int pageSize);
        Task<(List<AuthUser>, int TotalCount)> GetPagedByRoleAsync(Guid role, int pageNumber, int pageSize);

        Task<AuthUser?> UpdateAsync(AuthUser user);
        Task<bool> ExistsByLoginAsync(string login);
        Task<bool> ExistsByEmailAsync(string email);
        Task<long> CountAsync();
        Task<long> CountByStatusAsync(bool status);
        Task<UserStatsDto> GetStatsAsync();

        Task DeleteAllAsync();
    }
}
