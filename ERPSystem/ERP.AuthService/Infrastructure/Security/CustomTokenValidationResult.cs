// TokenValidationResult.cs
using System.Security.Claims;

namespace ERP.AuthService.Application.Interfaces
{
    /// <summary>
    /// Detailed token validation result
    /// </summary>
    public class CustomTokenValidationResult
    {
        private CustomTokenValidationResult(
            bool isValid,
            string? errorMessage,
            bool isExpired,
            ClaimsPrincipal? principal,
            Guid? userId,
            string? login,
            string? role,
            DateTime? issuedAt,
            DateTime? expiresAt)
        {
            IsValid = isValid;
            ErrorMessage = errorMessage;
            IsExpired = isExpired;
            Principal = principal;
            UserId = userId;
            Login = login;
            Role = role;
            IssuedAt = issuedAt;
            ExpiresAt = expiresAt;
        }

        public bool IsValid { get; }
        public string? ErrorMessage { get; }
        public bool IsExpired { get; }
        public ClaimsPrincipal? Principal { get; }
        public Guid? UserId { get; }
        public string? Login { get; }
        public string? Role { get; }
        public DateTime? IssuedAt { get; }
        public DateTime? ExpiresAt { get; }

        public static CustomTokenValidationResult Valid(
            ClaimsPrincipal principal,
            Guid? userId,
            string? login,
            string? role,
            DateTime issuedAt,
            DateTime expiresAt)
        {
            return new CustomTokenValidationResult(
                true, null, false, principal, userId, login, role, issuedAt, expiresAt);
        }

        public static CustomTokenValidationResult Invalid(string errorMessage, bool isExpired = false)
        {
            return new CustomTokenValidationResult(
                false, errorMessage, isExpired, null, null, null, null, null, null);
        }
    }
}