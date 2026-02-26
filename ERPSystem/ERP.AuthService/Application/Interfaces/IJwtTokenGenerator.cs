using ERP.AuthService.Domain;

namespace ERP.AuthService.Application.Interfaces
{
    public interface IJwtTokenGenerator
    {
        (string Token, DateTime ExpiresAt) GenerateAccessToken(
            Guid userId,
            string email,
            RoleEnum role,
            IEnumerable<string> privileges);

        string GenerateRefreshToken();
    }
}
