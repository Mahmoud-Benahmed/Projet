namespace ERP.AuthService.Application.DTOs.AuthUser
{
    public record AuthUserGetResponseDto
    (
        Guid Id,
        string Email,
        Guid RoleId,
        string RoleName,
        bool MustChangePassword,
        bool IsActive,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        DateTime? LastLoginAt
    );
}
