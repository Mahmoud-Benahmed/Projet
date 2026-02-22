using ERP.AuthService.Application.DTOs;
using ERP.AuthService.Application.Exceptions;
using ERP.AuthService.Application.Interfaces;
using ERP.AuthService.Domain;
using ERP.UserService.Application.Exceptions;
using Microsoft.AspNetCore.Identity;
using System.Security;
using System.Security.Cryptography;
using System.Text;


namespace ERP.AuthService.Application.Services
{
    public class AuthUserService: IAuthUserService
    {
        private readonly IAuthUserRepository _userRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IJwtTokenGenerator _jwtGenerator;
        private readonly IPasswordHasher<AuthUser> _passwordHasher;

        public AuthUserService(
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

        public async Task<AuthUserGetResponseDto> GetByIdAsync(Guid id)
        {
            var user= await _userRepository.GetByIdAsync(id)
                      ?? throw new UserNotFoundException(id);

            return MapToDto(user);
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


        public async Task RevokeRefreshTokenAsync(string refreshToken)
        {
            var token = await _refreshTokenRepository.GetByTokenAsync(refreshToken)
               ?? throw new UnauthorizedAccessException("Invalid refresh token.");

            await RevokeRefreshTokenAsyncPrivate(token);
        }

        public async Task ChangeAuthPasswordAsync(Guid id, string currPassword, string newPassword)
        {
            var user = await _userRepository.GetByIdAsync(id) 
                        ?? throw new UserNotFoundException(id);
            
            
            if (CryptographicOperations.FixedTimeEquals(
                                Encoding.UTF8.GetBytes(currPassword),
                                Encoding.UTF8.GetBytes(newPassword))
                )
            {
                throw new ArgumentException("The new password cannot be the same as the current password.");
            }


            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, currPassword);

            if (result == PasswordVerificationResult.Failed)
                throw new InvalidCredentialsException();

            var newHashedPassword = _passwordHasher.HashPassword(user, newPassword);
            user.ChangePassword(newHashedPassword);
            
            await _userRepository.UpdateAsync(user);
        }

        public async Task ChangePasswordByAdminAsync(Guid userId, string newPassword, Guid adminId)
        {

            var admin = await _userRepository.GetByIdAsync(adminId)
                        ?? throw new Exception("Internal server error");

            if (!admin.Role.Equals(UserRole.SystemAdmin))
                throw new UnauthorizedOperationException("Only admins can change passwords.");

            var user = await _userRepository.GetByIdAsync(userId)
                       ?? throw new UserNotFoundException(userId);

            var hashedNewPassword = _passwordHasher.HashPassword(user, newPassword);
            
            user.ChangePassword(hashedNewPassword);

            await _userRepository.UpdateAsync(user);
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

        private static AuthUserGetResponseDto MapToDto(AuthUser user)
        {
            return new AuthUserGetResponseDto
            (
                Id: user.Id,
                Email: user.Email,
                Role: user.Role,
                MustChangePassword: user.MustChangePassword,
                IsActive: user.IsActive,
                CreatedAt: user.CreatedAt,
                UpdatedAt: user.UpdatedAt,
                LastLoginAt: user.LastLoginAt
            );
        }

    }
}
