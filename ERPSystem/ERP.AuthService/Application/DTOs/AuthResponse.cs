namespace ERP.AuthService.Application.DTOs
{
    public record AuthResponse(
        string AccessToken,
        string RefreshToken,
        bool MustChangePassword,
        DateTime ExpiresAt
    );
}
