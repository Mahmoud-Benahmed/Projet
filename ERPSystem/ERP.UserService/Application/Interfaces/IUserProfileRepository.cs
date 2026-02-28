using ERP.UserService.Application.DTOs;
using ERP.UserService.Domain;

public interface IUserProfileRepository
{
    Task<UserProfile?> GetByIdAsync(Guid id);
    Task<UserProfile?> GetByAuthUserIdAsync(Guid authUserId);
    Task<UserProfile?> GetByLoginAsync(string login);

    Task<List<UserProfile>> GetAllAsync();

    Task<bool> ExistsByAuthUserIdAsync(Guid authUserId);
    Task<bool> ExistsByLoginAsync(string login);
    Task<bool> ExistsByEmailAsync(string email);

    Task AddAsync(UserProfile profile);
    void Remove(UserProfile profile);

    Task SaveChangesAsync();

    Task<(List<UserProfile> Items, int TotalCount)> GetPagedByStatusAsync(bool isActive, int pageNumber, int pageSize);

    Task<UserStatsDto> GetStatsAsync();
}