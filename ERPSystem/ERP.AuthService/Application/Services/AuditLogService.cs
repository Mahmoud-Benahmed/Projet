using ERP.AuthService.Application.DTOs.AuditLog;
using ERP.AuthService.Application.DTOs.AuthUser;
using ERP.AuthService.Application.Interfaces.Repositories;
using ERP.AuthService.Application.Interfaces.Services;
using ERP.AuthService.Domain.Logger;

namespace ERP.AuthService.Application.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly IAuditLogRepository _repository;

        public AuditLogService(IAuditLogRepository repository)
        {
            _repository = repository;
        }

        /// <summary>Get all audit logs, paginated, sorted by most recent.</summary>
        public async Task<PagedResultDto<AuditLogResponseDto>> GetAllAsync(int pageNumber, int pageSize)
        {
            var items = await _repository.GetAllAsync(pageNumber, pageSize);
            var total = await _repository.CountAsync();
            return new PagedResultDto<AuditLogResponseDto>(
                items.Select(MapToDto).ToList(),
                (int)total,
                pageNumber,
                pageSize);
        }

        /// <summary>Get all logs where the user is either the performer or the target.</summary>
        public async Task<PagedResultDto<AuditLogResponseDto>> GetByUserAsync(Guid userId, int pageNumber, int pageSize)
        {
            var items = await _repository.GetByUserAsync(userId, pageNumber, pageSize);
            return new PagedResultDto<AuditLogResponseDto>(
                items.Select(MapToDto).ToList(),
                items.Count,
                pageNumber,
                pageSize);
        }

        /// <summary>Get total count of all audit log entries.</summary>
        public async Task<long> CountAsync()
            => await _repository.CountAsync();

        /// <summary>Clear all audit logs (development only).</summary>
        public async Task ClearAsync()
            => await _repository.ClearAsync();

        // ── Mapping ───────────────────────────────────────────────────────────

        private static AuditLogResponseDto MapToDto(AuditLog log) =>
            new(
                log.Id,
                log.Action,
                log.PerformedBy,
                log.TargetUserId,
                log.Success,
                log.FailureReason,
                log.IpAddress,
                log.UserAgent,
                log.Metadata,
                log.Timestamp
            );
    }
}