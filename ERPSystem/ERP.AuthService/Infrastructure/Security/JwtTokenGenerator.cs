using ERP.AuthService.Application.Interfaces;
using ERP.AuthService.Domain;
using ERP.AuthService.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ERP.AuthService.Infrastructure.Security
{
    public class JwtTokenGenerator : IJwtTokenGenerator
    {
        private readonly JwtSettings _jwtSettings;
        private const string CLAIM_LOGIN = "login";
        private const string CLAIM_ROLE = "role";
        private const string CLAIM_PRIVILEGE = "privilege";

        public JwtTokenGenerator(IOptions<JwtSettings> jwtSettings)
        {
            _jwtSettings = jwtSettings.Value;
        }

        public (string Token, DateTime ExpiresAt) GenerateAccessToken(
                Guid userId,
                string login,
                string role,
                IEnumerable<string> privileges,
                UserSettings settings)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_jwtSettings.Secret));
            key.KeyId = "erp-key-1";

            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(CLAIM_LOGIN, login),
                new Claim(CLAIM_ROLE, role),
                new Claim("theme", settings.Theme.ToString()),
                new Claim("language", settings.Language.ToString()),
                new Claim(JwtRegisteredClaimNames.Iat,
                    new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(),
                    ClaimValueTypes.Integer64)
            }
            .Concat(privileges.Select(p => new Claim(CLAIM_PRIVILEGE, p)))
            .ToList();

            var expires = DateTime.UtcNow
                .AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: expires,
                signingCredentials: credentials);

            return (new JwtSecurityTokenHandler().WriteToken(token), expires);
        }

        public string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        /// <summary>
        /// Validates a JWT token and returns the ClaimsPrincipal if valid
        /// </summary>
        /// <param name="token">The JWT token to validate</param>
        /// <returns>ClaimsPrincipal if valid, null if invalid</returns>
        public ClaimsPrincipal? ValidateToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            var tokenHandler = new JwtSecurityTokenHandler();

            // Configure validation parameters
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidAudience = _jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(_jwtSettings.Secret)),
                ClockSkew = TimeSpan.FromMinutes(5),

                // Map claims correctly
                NameClaimType = JwtRegisteredClaimNames.Sub,
                RoleClaimType = CLAIM_ROLE
            };

            try
            {
                // Validate token
                var principal = tokenHandler.ValidateToken(
                    token,
                    validationParameters,
                    out var validatedToken);

                // Additional validation: ensure token is JWT and algorithm is correct
                if (validatedToken is JwtSecurityToken jwtToken &&
                    jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
                        StringComparison.InvariantCultureIgnoreCase))
                {
                    return principal;
                }

                return null;
            }
            catch (SecurityTokenExpiredException)
            {
                // Token expired - rethrow for specific handling
                throw;
            }
            catch (SecurityTokenValidationException)
            {
                // Invalid signature or other validation failure
                return null;
            }
            catch (Exception)
            {
                // Any other exception
                return null;
            }
        }

        /// <summary>
        /// Validates a token and returns detailed validation result
        /// </summary>
        public CustomTokenValidationResult ValidateTokenWithDetails(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return CustomTokenValidationResult.Invalid("Token is null or empty");

            var tokenHandler = new JwtSecurityTokenHandler();

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidAudience = _jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(_jwtSettings.Secret)),
                ClockSkew = TimeSpan.FromMinutes(5),
                NameClaimType = JwtRegisteredClaimNames.Sub,
                RoleClaimType = CLAIM_ROLE
            };

            try
            {
                var principal = tokenHandler.ValidateToken(
                    token,
                    validationParameters,
                    out var validatedToken);

                if (validatedToken is not JwtSecurityToken jwtToken ||
                    !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
                        StringComparison.InvariantCultureIgnoreCase))
                {
                    return CustomTokenValidationResult.Invalid("Invalid token algorithm");
                }

                // Extract claims
                var userIdClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub);
                var loginClaim = principal.FindFirst(CLAIM_LOGIN);
                var roleClaim = principal.FindFirst(CLAIM_ROLE);

                var userId = userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var uid)
                    ? uid : (Guid?)null;

                return CustomTokenValidationResult.Valid(
                    principal,
                    userId,
                    loginClaim?.Value,
                    roleClaim?.Value,
                    jwtToken.ValidFrom,
                    jwtToken.ValidTo);
            }
            catch (SecurityTokenExpiredException ex)
            {
                return CustomTokenValidationResult.Invalid($"Token expired: {ex.Message}",
                    isExpired: true);
            }
            catch (SecurityTokenInvalidSignatureException ex)
            {
                return CustomTokenValidationResult.Invalid($"Invalid signature: {ex.Message}");
            }
            catch (SecurityTokenInvalidIssuerException ex)
            {
                return CustomTokenValidationResult.Invalid($"Invalid issuer: {ex.Message}");
            }
            catch (SecurityTokenInvalidAudienceException ex)
            {
                return CustomTokenValidationResult.Invalid($"Invalid audience: {ex.Message}");
            }
            catch (Exception ex)
            {
                return CustomTokenValidationResult.Invalid($"Validation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Extracts claims from a token without validating signature (for debugging)
        /// </summary>
        public JwtSecurityToken? ReadToken(string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                if (handler.CanReadToken(token))
                {
                    return handler.ReadJwtToken(token);
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Checks if a token is expired without full validation
        /// </summary>
        public bool IsTokenExpired(string token)
        {
            var jwtToken = ReadToken(token);
            if (jwtToken == null)
                return true;

            var expiration = jwtToken.ValidTo;
            return expiration <= DateTime.UtcNow;
        }

        /// <summary>
        /// Gets the remaining validity of a token in seconds
        /// </summary>
        public double GetTokenRemainingLifetime(string token)
        {
            var jwtToken = ReadToken(token);
            if (jwtToken == null)
                return 0;

            var expiration = jwtToken.ValidTo;
            var remaining = expiration - DateTime.UtcNow;

            return remaining.TotalSeconds > 0 ? remaining.TotalSeconds : 0;
        }
    }
}