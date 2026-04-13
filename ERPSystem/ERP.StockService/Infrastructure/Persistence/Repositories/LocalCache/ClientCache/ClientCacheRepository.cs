using ERP.StockService.Application.Interfaces;
using ERP.StockService.Domain.LocalCache.Client;
using Microsoft.EntityFrameworkCore;

namespace ERP.StockService.Infrastructure.Persistence.Repositories.LocalCache;

public class ClientCacheRepository : IClientCacheRepository
{
    private readonly StockDbContext _dbContext;

    public ClientCacheRepository(StockDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Domain.LocalCache.Client.ClientCache?> GetByIdAsync(Guid id)
    {
        return await _dbContext.ClientCaches
            .Include(c => c.ClientCategories)
            .ThenInclude(cc => cc.Category)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task DeleteAsync(Domain.LocalCache.Client.ClientCache client)
    {
        if (client == null)
            throw new ArgumentNullException(nameof(client));

        _dbContext.ClientCaches.Remove(client); // Use correct DbSet
        await _dbContext.SaveChangesAsync();
    }

    public async Task<Domain.LocalCache.Client.ClientCache?> GetByNameAsync(string name)
    {
        return await _dbContext.ClientCaches
            .Include(c => c.ClientCategories)
            .ThenInclude(cc => cc.Category)
            .FirstOrDefaultAsync(c => c.Name == name && !c.IsDeleted);
    }

    public async Task<Domain.LocalCache.Client.ClientCache?> GetByEmailAsync(string email)
    {
        return await _dbContext.ClientCaches
            .Include(c => c.ClientCategories)
            .ThenInclude(cc => cc.Category)
            .FirstOrDefaultAsync(c => c.Email == email && !c.IsDeleted);
    }

    public async Task<List<Domain.LocalCache.Client.ClientCache>> GetAllAsync()
    {
        return await _dbContext.ClientCaches
            .Include(c => c.ClientCategories)
            .ThenInclude(cc => cc.Category)
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<List<Domain.LocalCache.Client.ClientCache>> GetActiveAsync()
    {
        return await _dbContext.ClientCaches
            .Include(c => c.ClientCategories)
            .ThenInclude(cc => cc.Category)
            .Where(c => !c.IsDeleted && !c.IsBlocked)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _dbContext.ClientCaches.AnyAsync(c => c.Id == id);
    }

    public async Task AddAsync(Domain.LocalCache.Client.ClientCache client)
    {
        await _dbContext.ClientCaches.AddAsync(client);
    }

    public Task UpdateAsync(Domain.LocalCache.Client.ClientCache client)
    {
        _dbContext.ClientCaches.Update(client);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _dbContext.SaveChangesAsync();
    }
}