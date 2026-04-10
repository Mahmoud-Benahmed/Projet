using ERP.AuthService.Domain.Logger;

namespace ERP.AuthService.Application.DTOs.AuditLog
{
    public record AuditLogResponseDto(
        Guid Id,
        AuditAction Action,
        Guid? PerformedBy,
        Guid? TargetUserId,
        bool Success,
        string? FailureReason,
        string? IpAddress,
        string? UserAgent,
        Dictionary<string, string>? Metadata,
        DateTime Timestamp
    );
}