using ERP.AuthService.Domain;

namespace ERP.AuthService.Application.Interfaces.Repositories
{
    public interface IRefreshTokenRepository
    {
        Task AddAsync(RefreshToken token);

        Task<RefreshToken?> GetByTokenAsync(string token);

        Task UpdateAsync(RefreshToken token);

        Task RevokeAllByUserIdAsync(Guid userId);
    }
}
