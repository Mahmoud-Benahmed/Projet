using ERP.AuthService.Application.DTOs;
using ERP.AuthService.Domain;

namespace ERP.AuthService.Application.Interfaces
{
    public interface IAuthUserService
    {
        Task<AuthUserGetResponseDto> GetByIdAsync(Guid id);
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> RefreshTokenAsync(string refreshToken);
        Task RevokeRefreshTokenAsync(string refreshToken);
        Task ChangeAuthPasswordAsync(Guid id, string currentPassword, string newPassword);
        Task ChangePasswordByAdminAsync(Guid userId, string newPassword, Guid adminId);
    }
}
