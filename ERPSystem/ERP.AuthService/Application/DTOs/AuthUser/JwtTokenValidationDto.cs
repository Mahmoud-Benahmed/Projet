namespace ERP.AuthService.Application.DTOs.AuthUser;

public record RefreshTokenValidationResultDto(bool IsValid, Guid? UserId, string? ExpirationReason);

public record TokenValidationResultDto(
    bool IsValid,
    Guid? UserId,
    string? Login,
    string? Email,
    string? FullName,
    Guid? RoleId,
    bool IsActive,
    string? ExpirationReason);