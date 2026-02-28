using ERP.UserService.Application.DTOs;

namespace ERP.UserService.Application.Interfaces
{
    public interface IUserProfileService
    {
        Task<UserProfileResponseDto> CreateProfileAsync(CreateUserProfileDto dto);
        Task<UserProfileResponseDto> GetByIdAsync(Guid id);
        Task<UserProfileResponseDto> GetByAuthUserIdAsync(Guid authUserId);
        Task<UserProfileResponseDto> GetByLoginAsync(string login);

        Task<bool> ExistsByAuthUserIdAsync(Guid authUserId);
        Task<bool> ExistsByLoginAsync(string login);

        Task<List<UserProfileResponseDto>> GetAllAsync();
        Task<PagedResultDto<UserProfileResponseDto>> GetPagedByStatusAsync(bool isActive, int pageNumber, int pageSize);
        Task<PagedResultDto<UserProfileResponseDto>> GetPagedByRoleAsync(string role, int pageNumber, int pageSize);

        Task<UserProfileResponseDto> CompleteProfileAsync(Guid authUserId, CompleteProfileDto dto);
        Task ActivateAsync(Guid id);
        Task DeactivateAsync(Guid id);
        Task DeleteAsync(Guid id);
        Task<UserStatsDto> GetStatsAsync();
    }
}
