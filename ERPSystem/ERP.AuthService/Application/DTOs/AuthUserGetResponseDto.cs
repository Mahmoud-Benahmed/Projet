using ERP.AuthService.Domain;

namespace ERP.AuthService.Application.DTOs
{
    public record AuthUserGetResponseDto
    (
        Guid Id,
        string Email,
        UserRole Role,
        bool MustChangePassword,
        bool IsActive,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        DateTime? LastLoginAt
    );
}
