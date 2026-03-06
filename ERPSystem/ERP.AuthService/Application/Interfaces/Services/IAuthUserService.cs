using ERP.AuthService.Application.DTOs;
using ERP.AuthService.Application.DTOs.AuthUser;

namespace ERP.AuthService.Application.Interfaces.Services
{
    public interface IAuthUserService
    {
        Task<AuthUserGetResponseDto> RegisterAsync(RegisterRequestDto request);
        Task<AuthResponseDto> LoginAsync(LoginRequestDto request);
        Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);
        Task RevokeRefreshTokenAsync(string refreshToken);
        Task ChangeAuthPasswordAsync(Guid id, string currentPassword, string newPassword);
        Task ChangePasswordByAdminAsync(Guid userId, string newPassword, Guid adminId);
        
        Task ActivateAsync(Guid authUserId);
        Task DeactivateAsync(Guid authUserId);

        Task<bool> ExistsByLogin(string login);
        Task<bool> ExistsByEmail(string email);

        Task<AuthUserGetResponseDto> UpdateProfile(Guid id, UpdateProfileDto request);

        Task<AuthUserGetResponseDto> GetByIdAsync(Guid id);
        Task<AuthUserGetResponseDto> GetByLoginAsync(string login);
        Task<PagedResultDto<AuthUserGetResponseDto>> GetAllAsync(int pageN, int pageSize); // <<<<<<<<<<<<<<<<<<<<<<<<<<<
        Task<PagedResultDto<AuthUserGetResponseDto>> GetPagedByStatusAsync(bool isActive, int pageNumber, int pageSize); // <<<<<<<<<<<<<<<<<<<<<<<<<<<
        Task<PagedResultDto<AuthUserGetResponseDto>> GetPagedByRoleAsync(Guid roleId, int pageNumber, int pageSize); // <<<<<<<<<<<<<<<<<<<<<<<<<<<


        Task<UserStatsDto> GetStatsAsync(); // <<<<<<<<<<<<<<<<<<<<<<<<<<<



    }
}
