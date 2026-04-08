using ERP.AuthService.Application.DTOs.AuditLog;
using ERP.AuthService.Application.DTOs.AuthUser;

namespace ERP.AuthService.Application.Interfaces.Services
{
    public interface IAuditLogService
    {
        Task<PagedResultDto<AuditLogResponseDto>> GetAllAsync(int pageNumber, int pageSize);
        Task<PagedResultDto<AuditLogResponseDto>> GetByUserAsync(Guid userId, int pageNumber, int pageSize);
        Task<long> CountAsync();
        Task ClearAsync();
    }
}