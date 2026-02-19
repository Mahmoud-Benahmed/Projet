using ERP.AuthService.Domain;

namespace ERP.AuthService.Application.Interfaces
{
    public interface IAuthUserRepository
    {
        Task AddAsync(AuthUser user);
        Task<AuthUser?> GetByEmailAsync(string email);
        Task<AuthUser?> GetByIdAsync(Guid id);
        Task UpdateAsync(AuthUser user);
        Task<bool> ExistsByEmailAsync(string email);

    }
}
