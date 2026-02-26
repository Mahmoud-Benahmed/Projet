using ERP.AuthService.Application.Interfaces;
using ERP.AuthService.Domain;
using ERP.AuthService.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ERP.AuthService.Infrastructure.Security
{
    public class JwtTokenGenerator : IJwtTokenGenerator
    {
        private readonly JwtSettings _jwtSettings;

        public JwtTokenGenerator(IOptions<JwtSettings> jwtSettings)
        {
            _jwtSettings = jwtSettings.Value;
        }

        public (string Token, DateTime ExpiresAt) GenerateAccessToken(
                Guid userId,
                string login,
                RoleEnum role,
                IEnumerable<string> privileges)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_jwtSettings.Secret));
            key.KeyId = "erp-key-1";

            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub,   userId.ToString()),
                new Claim("login", login),
                new Claim("role",                        role.ToString()),
            };

            // add each privilege as a separate claim
            foreach (var privilege in privileges)
            {
                claims.Add(new Claim("privilege", privilege));
            }

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
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        }
    }
}
