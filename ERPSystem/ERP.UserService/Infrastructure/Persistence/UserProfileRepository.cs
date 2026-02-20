using ERP.UserService.Domain;
using Microsoft.EntityFrameworkCore;

namespace ERP.UserService.Infrastructure.Persistence;

public class UserProfileRepository : IUserProfileRepository
{
    private readonly UserDbContext _context;

    public UserProfileRepository(UserDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(UserProfile profile)
    {
        await _context.UserProfiles.AddAsync(profile);
    }

    public async Task<UserProfile?> GetByIdAsync(Guid id)
    {
        return await _context.UserProfiles
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<UserProfile?> GetByAuthUserIdAsync(Guid authUserId)
    {
        return await _context.UserProfiles
            .FirstOrDefaultAsync(x => x.AuthUserId == authUserId);
    }

    public async Task<List<UserProfile>> GetAllAsync()
    {
        return await _context.UserProfiles.ToListAsync();
    }

    public void Remove(UserProfile profile)
    {
        _context.UserProfiles.Remove(profile);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<(List<UserProfile> Items, int TotalCount)>
    GetPagedByStatusAsync(bool isActive, int pageNumber, int pageSize)
    {
        var query = _context.UserProfiles
            .Where(x => x.IsActive == isActive);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(x => x.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

}