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
                IEnumerable<string> privileges)
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
    }
}
