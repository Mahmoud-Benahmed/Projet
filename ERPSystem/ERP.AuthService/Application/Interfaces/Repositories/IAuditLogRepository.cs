using ERP.AuthService.Domain.Logger;

namespace ERP.AuthService.Application.Interfaces.Repositories
{
    public interface IAuditLogRepository
    {
        Task AddAsync(AuditLog log);
        Task<List<AuditLog>> GetByUserAsync(Guid userId, int pageNumber, int pageSize);
        Task<List<AuditLog>> GetAllAsync(int pageNumber, int pageSize);
        Task<long> CountAsync();
        Task ClearAsync();

    }
}