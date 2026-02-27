using ERP.AuthService.Application.DTOs.AuthUser;
//using ERP.AuthService.Application.Events;
using ERP.AuthService.Application.Exceptions.AuthUser;
using ERP.AuthService.Application.Interfaces;
using ERP.AuthService.Application.Interfaces.Repositories;
using ERP.AuthService.Application.Interfaces.Services;
using ERP.AuthService.Domain;
using ERP.AuthService.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Identity;
using System.Security;
using System.Security.Cryptography;
using System.Text;


namespace ERP.AuthService.Application.Services
{
    public class AuthUserService : IAuthUserService
    {
        private readonly IAuthUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IJwtTokenGenerator _jwtGenerator;
        private readonly IPasswordHasher<AuthUser> _passwordHasher;
        private readonly IControleRepository _controleRepository;
        private readonly IPrivilegeRepository _privilegeRepository;
        //private readonly IEventPublisher _eventPublisher;

        public AuthUserService(
            IAuthUserRepository userRepository,
            IRoleRepository roleRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IJwtTokenGenerator jwtGenerator,
            IPasswordHasher<AuthUser> passwordHasher,
            IControleRepository controleRepository,
            IPrivilegeRepository privilegeRepository
            )
            //IEventPublisher eventPublisher
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _jwtGenerator = jwtGenerator;
            _passwordHasher = passwordHasher;
            _controleRepository = controleRepository;
            _privilegeRepository = privilegeRepository;
            //_eventPublisher = eventPublisher;
        }

        public async Task<AuthUserGetResponseDto> GetByIdAsync(Guid id)
        {
            var user = await _userRepository.GetByIdAsync(id)
                      ?? throw new UserNotFoundException(id);

            return await MapToDtoAsync(user);
        }

        public async Task<AuthUserGetResponseDto> GetByLoginAsync(string login)
        {
            var user= await _userRepository.GetByLoginAsync(login)
                        ?? throw new UserNotFoundException(login);

            return await MapToDtoAsync(user);
        }

        public async Task<AuthUserGetResponseDto> RegisterAsync(RegisterRequestDto request)
        {
            if (await _userRepository.ExistsByLoginAsync(request.Login))
                throw new LoginAlreadyExsistException();

            if (await _userRepository.ExistsByEmailAsync(request.Email))
                throw new EmailAlreadyExistsException();

            var role = await _roleRepository.GetByIdAsync(request.RoleId) ?? throw new InvalidOperationException("Role not found.");

            var user = new AuthUser(request.Login, request.Email);
            var hashedPassword = _passwordHasher.HashPassword(user, request.Password);
            user.SetPasswordHash(hashedPassword);
            user.SetRole(role.Id);

            await _userRepository.AddAsync(user);

            // publish event to Kafka
            //await _eventPublisher.PublishAsync("UserRegistered", new UserRegisteredEvent(
            //    AuthUserId: user.Id.ToString(),
            //    Email: user.Email
            //));

            return await MapToDtoAsync(user);
        }

        public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
        {
            var user = await _userRepository.GetByLoginAsync(request.Login)
                ?? throw new InvalidCredentialsException();

            if (!user.CanLogin())
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


        public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
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

            if (user is null)
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

        private async Task<AuthResponseDto> GenerateAuthResponseAsync(AuthUser user)
        {
            var role = await _roleRepository.GetByIdAsync(user.RoleId)
                       ?? throw new InvalidOperationException("Role not found.");

            // fetch granted privileges for this role
            var privileges = await _privilegeRepository.GetByRoleIdAsync(user.RoleId);
            var grantedPrivileges = privileges
                .Where(p => p.IsGranted)
                .Select(p => p.ControleId.ToString())
                .ToList();

            // resolve controle names for the privileges
            var controles = await _controleRepository.GetAllAsync();
            var controleMap = controles.ToDictionary(c => c.Id, c => c.Libelle);

            var privilegeNames = privileges
                .Where(p => p.IsGranted)
                .Select(p => controleMap.TryGetValue(p.ControleId, out var name) ? name : null)
                .Where(name => name != null)
                .Select(name => name!)
                .ToList();

            var (accessToken, expiresAt) = _jwtGenerator.GenerateAccessToken(
                user.Id,
                user.Login,
                role.Libelle,
                privilegeNames
            );

            var refreshTokenValue = _jwtGenerator.GenerateRefreshToken();
            var refreshToken = new RefreshToken(
                user.Id,
                refreshTokenValue,
                DateTime.UtcNow.AddDays(7)
            );

            await _refreshTokenRepository.AddAsync(refreshToken);

            return new AuthResponseDto(
                accessToken,
                refreshTokenValue,
                user.MustChangePassword,
                expiresAt
            );
        }
        private async Task<AuthUserGetResponseDto> MapToDtoAsync(AuthUser user)
        {
            var role = await _roleRepository.GetByIdAsync(user.RoleId)
                       ?? throw new InvalidOperationException("Role not found.");

            return new AuthUserGetResponseDto(
                Id: user.Id,
                Email: user.Email,
                Login: user.Login,
                RoleId: user.RoleId,
                RoleName: role.Libelle.ToString(),
                MustChangePassword: user.MustChangePassword,
                IsActive: user.IsActive,
                CreatedAt: user.CreatedAt,
                UpdatedAt: user.UpdatedAt,
                LastLoginAt: user.LastLoginAt
            );
        }
    }
}
