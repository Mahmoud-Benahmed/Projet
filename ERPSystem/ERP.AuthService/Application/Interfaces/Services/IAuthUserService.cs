using ERP.AuthService.Application.DTOs.AuthUser;

namespace ERP.AuthService.Application.Interfaces.Services
{
    public interface IAuthUserService
    {
        Task<AuthUserGetResponseDto> GetByIdAsync(Guid id);
        Task<AuthUserGetResponseDto> GetByLoginAsync(string login);
        Task<AuthUserGetResponseDto> RegisterAsync(RegisterRequestDto request);
        Task<AuthResponseDto> LoginAsync(LoginRequestDto request);
        Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);

        Task ActivateAsync(Guid authUserId);
        Task DeactivateAsync(Guid authUserId);

        Task<bool> ExistsByLogin(string login);
        Task<bool> ExistsByEmail(string email);

        Task RevokeRefreshTokenAsync(string refreshToken);
        Task ChangeAuthPasswordAsync(Guid id, string currentPassword, string newPassword);
        Task ChangePasswordByAdminAsync(Guid userId, string newPassword, Guid adminId);
    }
}
