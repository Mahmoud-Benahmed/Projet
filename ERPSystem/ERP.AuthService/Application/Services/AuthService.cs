using ERP.AuthService.Application.DTOs;
using ERP.AuthService.Application.Exceptions;
using ERP.AuthService.Application.Interfaces;
using ERP.AuthService.Domain;
using Microsoft.AspNetCore.Identity;
using System.Security;


namespace ERP.AuthService.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAuthUserRepository _userRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IJwtTokenGenerator _jwtGenerator;
        private readonly IPasswordHasher<AuthUser> _passwordHasher;

        public AuthService(
            IAuthUserRepository userRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IJwtTokenGenerator jwtGenerator,
            IPasswordHasher<AuthUser> passwordHasher)
        {
            _userRepository = userRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _jwtGenerator = jwtGenerator;
            _passwordHasher = passwordHasher;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            if (await _userRepository.ExistsByEmailAsync(request.Email))
                throw new EmailAlreadyExistsException();

            var user = new AuthUser(request.Email);
            var hashedPassword = _passwordHasher.HashPassword(user, request.Password);
            user.SetPasswordHash(hashedPassword);
            user.SetRole(request.Role!.Value);

            await _userRepository.AddAsync(user);

            return await GenerateAuthResponseAsync(user);
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email)
                ?? throw new InvalidCredentialsException();

            if (!user.IsActive)
                throw new UserInactiveException();

            var result = _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                request.Password
            );

            if (result == PasswordVerificationResult.Failed)
                throw new InvalidCredentialsException();

            user.RecordLogin();
            await _userRepository.UpdateAsync(user);
            return await GenerateAuthResponseAsync(user);
        }


        public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
        {
            var token = await _refreshTokenRepository.GetByTokenAsync(refreshToken)
                ?? throw new UnauthorizedAccessException("Invalid refresh token.");

            if (token.IsExpired())
                throw new UnauthorizedAccessException("Refresh token expired.");

            if (token.IsRevoked)
            {
                // Possible token reuse attack
                await _refreshTokenRepository.RevokeAllByUserIdAsync(token.UserId);
                throw new SecurityException("Reusing revoked tokens is NOT ALLOWED");
            }

            var user = await _userRepository.GetByIdAsync(token.UserId);
            
            if(user is null)
            {
                await _refreshTokenRepository.RevokeAllByUserIdAsync(token.UserId);
                throw new UnauthorizedAccessException("User associated with the refresh token NOT FOUND");
            }

            // Rotate token
            await RevokeRefreshTokenAsyncPrivate(token);// revoke the token to refresh before getting a fresh one

            return await GenerateAuthResponseAsync(user);
        }


        private async Task RevokeRefreshTokenAsyncPrivate(RefreshToken token)
        {
            var user = await _userRepository.GetByIdAsync(token.UserId);

            if (user is null)
            {
                await _refreshTokenRepository.RevokeAllByUserIdAsync(token.UserId);
                throw new UnauthorizedAccessException("User associated with the refresh token NOT FOUND");
            }

            if (token.IsRevoked)
                throw new TokenAlreadyRevokedException();

            token.Revoke();
            await _refreshTokenRepository.UpdateAsync(token);
        }

        public async Task RevokeRefreshTokenAsync(string refreshToken)
        {
            var token = await _refreshTokenRepository.GetByTokenAsync(refreshToken)
               ?? throw new UnauthorizedAccessException("Invalid refresh token.");

            await RevokeRefreshTokenAsyncPrivate(token);
        }

        public async Task ChangePasswordAsync(Guid id, string currentPassword, string newPassword)
        {
            if (currentPassword.Equals(newPassword))
                throw new ArgumentException("New password must be different from current password.");

            var user = await _userRepository.GetByIdAsync(id) ??
                throw new UserNotFoundException(id);

            // verify current password before allowing change
            var result = _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                currentPassword);

            if (result == PasswordVerificationResult.Failed)
                throw new InvalidCredentialsException();

            // hash the new password before storing
            var hashedNewPassword = _passwordHasher.HashPassword(user, newPassword);
            user.ChangePassword(hashedNewPassword);

            await _userRepository.UpdateAsync(user);
        }

        private async Task<AuthResponse> GenerateAuthResponseAsync(AuthUser user)
        {
            var (accessToken, expiresAt) = _jwtGenerator.GenerateAccessToken(
                user.Id,
                user.Email,
                user.Role
            );

            var refreshTokenValue = _jwtGenerator.GenerateRefreshToken();

            var refreshToken = new RefreshToken(
                user.Id,
                refreshTokenValue,
                DateTime.UtcNow.AddDays(7)
            );

            await _refreshTokenRepository.AddAsync(refreshToken);

            return new AuthResponse(
                accessToken,
                refreshTokenValue,
                expiresAt
            );
        }

    }
}
