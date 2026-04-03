using Confluent.Kafka;
using ERP.AuthService.Application.DTOs;
using ERP.AuthService.Application.DTOs.AuthUser;
using ERP.AuthService.Application.Events;
using ERP.AuthService.Application.Exceptions;
using ERP.AuthService.Application.Exceptions.AuthUser;
using ERP.AuthService.Application.Interfaces;
using ERP.AuthService.Application.Interfaces.Repositories;
using ERP.AuthService.Application.Interfaces.Services;
using ERP.AuthService.Domain;
using ERP.AuthService.Domain.Logger;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using System.Globalization;
using System.Security;
using System.Security.Cryptography;
using System.Text;


namespace ERP.AuthService.Application.Services
{
    public class AuthUserService : IAuthUserService
    {
        private readonly IAuditLogger _auditLogger;
        private readonly IHttpContextAccessor _httpContext;
        private readonly IAuthUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IJwtTokenGenerator _jwtGenerator;
        private readonly IPasswordHasher<AuthUser> _passwordHasher;
        private readonly IControleRepository _controleRepository;
        private readonly IPrivilegeRepository _privilegeRepository;
        //private readonly IEventPublisher _eventPublisher;

        public AuthUserService(
            IAuditLogger auditLogger,
            IHttpContextAccessor httpContextAccessor,
            IAuthUserRepository userRepository,
            IRoleRepository roleRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IJwtTokenGenerator jwtGenerator,
            IPasswordHasher<AuthUser> passwordHasher,
            IControleRepository controleRepository,
            IPrivilegeRepository privilegeRepository
            )
            //,IEventPublisher eventPublisher
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _jwtGenerator = jwtGenerator;
            _passwordHasher = passwordHasher;
            _controleRepository = controleRepository;
            _privilegeRepository = privilegeRepository;
            _auditLogger = auditLogger;
            _httpContext = httpContextAccessor;
            //_eventPublisher = eventPublisher;
        }

        // ============================================
        // READ
        // ============================================

        public async Task<PagedResultDto<AuthUserGetResponseDto>> GetAllAsync(int pageNumber, int pageSize, Guid? excludeId)
        {
            ValidatePaging(pageNumber, pageSize);

            var (items, totalCount)= await _userRepository.GetAllAsync(pageNumber, pageSize, excludeId);

            var mapped = await Task.WhenAll(items.Select(MapToDtoAsync));

            return new PagedResultDto<AuthUserGetResponseDto>(
                mapped,
                totalCount,
                pageNumber,
                pageSize);
        }

        public async Task<PagedResultDto<AuthUserGetResponseDto>> GetPagedByStatusAsync(bool isActive, int pageNumber, int pageSize, Guid? excludeId)
        {

            ValidatePaging(pageNumber, pageSize);

            var (items, totalCount) = await _userRepository.GetPagedByStatusAsync(isActive, pageNumber, pageSize, excludeId);

            var mapped = await Task.WhenAll(items.Select(MapToDtoAsync));

            return new PagedResultDto<AuthUserGetResponseDto>(
                mapped,
                totalCount,
                pageNumber,
                pageSize);
        }

        public async Task<PagedResultDto<AuthUserGetResponseDto>> GetPagedByRoleAsync(Guid roleId, int pageNumber, int pageSize, Guid? excludeId)
        {

            ValidatePaging(pageNumber, pageSize);

            var (items, totalCount) = await _userRepository.GetPagedByRoleAsync(roleId, pageNumber, pageSize, excludeId);

            var mapped = await Task.WhenAll(items.Select(MapToDtoAsync));

            return new PagedResultDto<AuthUserGetResponseDto>(
                mapped,
                totalCount,
                pageNumber,
                pageSize);
        }

        public async Task<AuthUserGetResponseDto> GetByIdAsync(Guid id)
        {
            var user = await _userRepository.GetByIdAsync(id)
                      ?? throw new UserNotFoundException(id);

            return await MapToDtoAsync(user);
        }

        public async Task<AuthUserGetResponseDto> GetByLoginAsync(string login)
        {
            var user = await _userRepository.GetByLoginAsync(login)
                        ?? throw new UserNotFoundException(login);

            return await MapToDtoAsync(user);
        }

        public async Task<bool> ExistsByEmail(string email)
        {
            return await _userRepository.ExistsByEmailAsync(email);
        }

        public async Task<bool> ExistsByLogin(string login)
        {
            return await _userRepository.ExistsByLoginAsync(login);
        }




        // ===============================
        // PROFILE UPDATE, CREATE, LOGIN
        // ===============================
        public async Task<AuthUserGetResponseDto> UpdateProfile(Guid id, UpdateProfileDto request)
        {
            var user= await _userRepository.GetByIdAsync(id) ?? throw new UserNotFoundException(id);
            user.UpdateProfile(request.FullName, request.Email);


            var updated= await _userRepository.UpdateAsync(user);

            await _auditLogger.LogAsync(
                AuditAction.ProfileUpdated,
                success: true,
                performedBy: id,
                targetUserId: id,
                metadata: new() { ["email"] = user.Email, ["fullName"] = user.FullName },
                ipAddress: GetIp());
            return await MapToDtoAsync(updated);
        }


        public async Task<AuthUserGetResponseDto> RegisterAsync(RegisterRequestDto request, Guid performedById)
        {
            if (await _userRepository.ExistsByLoginAsync(request.Login))
                throw new LoginAlreadyExsistException();

            if (await _userRepository.ExistsByEmailAsync(request.Email))
                throw new EmailAlreadyExistsException();

            var role = await _roleRepository.GetByIdAsync(request.RoleId) ?? throw new InvalidOperationException("Role not found.");

            string capitalizedFullName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(request.FullName.ToLower());

            var user = new AuthUser(request.Login, request.Email, capitalizedFullName, role.Id);
            var hashedPassword = _passwordHasher.HashPassword(user, request.Password);
            user.SetPasswordHash(hashedPassword);

            await _userRepository.AddAsync(user);

            await _auditLogger.LogAsync(
                AuditAction.UserRegistered,
                success: true,
                performedBy: performedById,
                targetUserId: user.Id,
                metadata: new() { ["login"] = request.Login, ["email"] = request.Email },
                ipAddress: GetIp());
            
            return await MapToDtoAsync(user);
        }
                
        public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
        {
            try
            {
                var user = await _userRepository.GetByLoginAsync(request.Login)
                    ?? throw new InvalidCredentialsException();

                if (!user.CanLogin())
                    throw new UserInactiveException("Sorry, you cannot login because your account is disabled.");


                var result = _passwordHasher.VerifyHashedPassword(
                    user,
                    user.PasswordHash,
                    request.Password
                );

                if (result == PasswordVerificationResult.Failed)
                    throw new InvalidCredentialsException();

                user.RecordLogin();
                await _userRepository.UpdateAsync(user);

                var token = await GenerateAuthResponseAsync(user);
                await _auditLogger.LogAsync(
                        AuditAction.Login,
                        success: true,
                        performedBy: user.Id,
                        metadata: new() { ["login"] = request.Login },
                        ipAddress: GetIp(),
                        userAgent: GetUserAgent());
                return token;
            }
            catch (InvalidCredentialsException ex)
            {
                await _auditLogger.LogAsync(
                        AuditAction.Login,
                        success: false,
                        failureReason: "Invalid credentials",
                        metadata: new() { ["login"] = request.Login },
                        ipAddress: GetIp(),
                        userAgent: GetUserAgent());
                throw;
            }
            catch (UserInactiveException)
            {
                await _auditLogger.LogAsync(
                    AuditAction.Login,
                    success: false,
                    failureReason: "Account inactive",
                    metadata: new() { ["login"] = request.Login },
                    ipAddress: GetIp(),
                    userAgent: GetUserAgent());
                throw;
            }
        }


        // ======================
        // TOKEN MANAGEMENT
        // ======================
        public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
        {
            var token = await _refreshTokenRepository.GetByTokenAsync(refreshToken)
                ?? throw new UnauthorizedAccessException("Invalid refresh token.");

            if (token.IsExpired())
                throw new UnauthorizedAccessException("Refresh token expired.");

            if (token.IsRevoked)
            {
                // Possible token reuse attack
                throw new TokenAlreadyRevokedException();
            }

            var user = await _userRepository.GetByIdAsync(token.UserId);

            if (user is null)
            {
                await _refreshTokenRepository.RevokeAllByUserIdAsync(token.UserId);
                throw new UnauthorizedAccessException("User associated with the refresh token NOT FOUND");
            }

            // Rotate token
            await RevokeRefreshTokenAsyncPrivate(token);// revoke the token to refresh before getting a fresh one

            var result=await GenerateAuthResponseAsync(user);

            await _auditLogger.LogAsync(
                    AuditAction.TokenRefreshed,
                    success: true,
                    performedBy: user.Id,
                    ipAddress: GetIp());

            return result;
        }

        public async Task RevokeRefreshTokenAsync(string refreshToken)
        {
            var token = await _refreshTokenRepository.GetByTokenAsync(refreshToken)
               ?? throw new UnauthorizedAccessException("Invalid refresh token.");

            await RevokeRefreshTokenAsyncPrivate(token);
            await _auditLogger.LogAsync(
                AuditAction.Logout,
                success: true,
                performedBy: token.UserId,
                ipAddress: GetIp());
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

            var privileges = await _privilegeRepository.GetByRoleIdAsync(user.RoleId);

            var grantedControleIds = privileges
                .Where(p => p.IsGranted)
                .Select(p => p.ControleId)
                .ToList();

            var controles = await _controleRepository.GetByIdsAsync(grantedControleIds);
            var privilegeNames = controles.Select(c => c.Libelle).ToList();

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

        // ======================
        // CHANGE PASSWORD
        // ======================
        public async Task ChangePasswordAsync(Guid id, ChangePasswordRequestDto request)
        {
            var user = await _userRepository.GetByIdAsync(id)
                        ?? throw new UserNotFoundException(id);

            if (!user.IsActive)
                throw new UserInactiveException();

            if (request.CurrentPassword.Equals(request.NewPassword))
                throw new ArgumentException("The new password cannot be the same as the current password.");
            
            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);
            if (result == PasswordVerificationResult.Failed)
                throw new InvalidCredentialsException();

            var newHashedPassword = _passwordHasher.HashPassword(user, request.NewPassword);
            user.ChangePassword(newHashedPassword);
            if (user.MustChangePassword) user.MustChangePassword = false;

            await _userRepository.UpdateAsync(user);

            await _auditLogger.LogAsync(
                  AuditAction.PasswordChanged,
                  success: true,
                  performedBy: id,
                  ipAddress: GetIp(),
                  metadata: new() { ["login"]= user.Login });
        }

        public async Task ChangePasswordByAdminAsync(Guid userId, AdminChangeProfileRequest request, Guid adminId)
        {
            var user = await _userRepository.GetByIdAsync(userId)
                       ?? throw new UserNotFoundException(userId);

            var hashedNewPassword = _passwordHasher.HashPassword(user, request.NewPassword);

            user.ChangePassword(hashedNewPassword);
            user.MustChangePassword = true;

            await _userRepository.UpdateAsync(user);
            await _auditLogger.LogAsync(
                    AuditAction.PasswordChangedByAdmin,
                        success: true,
                        performedBy: adminId,
                        targetUserId: userId,
                        ipAddress: GetIp(),
                        metadata: new() { ["login"] = user.Login });
        }


        // ======================
        // ACTIVATE/DEACTIVATE 
        // ======================
        public async Task ActivateAsync(Guid authUserId, Guid performedById)
        {
            if (authUserId == performedById)
                throw new UnauthorizedOperationException("You cannot apply this operation on your account.");
            var user = await _userRepository.GetByIdAsync(authUserId)
                       ?? throw new UserNotFoundException(authUserId);


            var performedBy= await _userRepository.GetByIdAsync(performedById)
                       ?? throw new UserNotFoundException(performedById);

            if (user.IsActive)
                throw new UserActiveException();
            user.Activate();
            await _userRepository.UpdateAsync(user);
            await _auditLogger.LogAsync(
                    AuditAction.UserActivated,
                    success: true,
                    performedBy: performedById,
                    targetUserId: user.Id,
                    ipAddress: GetIp());
        }

        public async Task DeactivateAsync(Guid authUserId, Guid performedById)
        {
            if (authUserId == performedById)
                throw new UnauthorizedOperationException("You cannot apply this operation on your account.");

            var user = await _userRepository.GetByIdAsync(authUserId)
                       ?? throw new UserNotFoundException(authUserId);
            if (!user.IsActive)
                throw new UserInactiveException();

            var performedBy = await _userRepository.GetByIdAsync(performedById)
                       ?? throw new UserNotFoundException(performedById);

            user.Deactivate();
            await _userRepository.UpdateAsync(user);
            await _auditLogger.LogAsync(
                    AuditAction.UserDeactivated,
                    success: true,
                    performedBy: performedById,
                    targetUserId: user.Id,
                    ipAddress: GetIp());
        }

        // ======================
        // SOFT DELETE
        // ======================
        public async Task SoftDeleteAsync(Guid deletedId, Guid performedById)
        {

            if (deletedId == performedById)
                throw new UnauthorizedOperationException("You cannot apply this operation on your account.");

            var user = await _userRepository.GetByIdAsync(deletedId)
                        ?? throw new UserNotFoundException(deletedId);

            var performedBy = await _userRepository.GetByIdAsync(performedById) ?? throw new UserNotFoundException(performedById);

            if (user.IsDeleted) return; // no need to update

            user.Delete();
            await _userRepository.UpdateAsync(user);

            await _auditLogger.LogAsync(
                    AuditAction.UserDeleted,
                    success: true,
                    performedBy: performedById,
                    targetUserId: user.Id,
                    ipAddress: GetIp(),
                    metadata: new() { ["deleted"] = user.Login, ["deletedBy"] = performedById.ToString()});
        }

        public async Task RestoreAsync(Guid deletedId, Guid performedById)
        {

            if (deletedId == performedById)
                throw new UnauthorizedOperationException("You cannot apply this operation on your account.");

            var user = await _userRepository.GetByDeletedIdAsync(deletedId)
                        ?? throw new UserNotFoundException(deletedId);

            var perfomedBy = await _userRepository.GetByIdAsync(performedById) ?? throw new UserNotFoundException(performedById);

            if (!user.IsDeleted) return; // no need to update
            user.Restore();
            await _userRepository.UpdateAsync(user);

            await _auditLogger.LogAsync(
                    AuditAction.UserRestored,
                    success: true,
                    performedBy: performedById,
                    targetUserId: user.Id,
                    ipAddress: GetIp(),
                    metadata: new() { ["restored"] = user.Login, ["restoredBy"] = performedById.ToString() });
        }

        public async Task<PagedResultDto<AuthUserGetResponseDto>> GetDeletedPagedAsync(int pageNumber, int pageSize, Guid? excludeId)
        {
            ValidatePaging(pageNumber, pageSize);
            var (items, totalCount) = await _userRepository.GetDeletedPagedAsync(pageNumber, pageSize, excludeId);

            var mapped = await Task.WhenAll(items.Select(MapToDtoAsync));

            return new PagedResultDto<AuthUserGetResponseDto>(
                mapped,
                totalCount,
                pageNumber,
                pageSize);
        }




        // ======================
        // STATS
        // ======================
        public async Task<UserStatsDto> GetStatsAsync(Guid? excludeId = default)
        {
            return await _userRepository.GetStatsAsync(excludeId);
        }


        // ======================
        // DTO MAPPING HELPER
        // ======================

        private async Task<AuthUserGetResponseDto> MapToDtoAsync(AuthUser user)
        {
            var role = await _roleRepository.GetByIdAsync(user.RoleId)
                       ?? throw new UnauthorizedAccessException("Role not found.");

            return new AuthUserGetResponseDto(
                Id: user.Id,
                Email: user.Email,
                FullName: user.FullName,
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


        private string? GetIp()
        {
            var ip = _httpContext?.HttpContext?.Connection.RemoteIpAddress;

            if (ip == null)
                return null;

            // Convert IPv6-mapped IPv4 to normal IPv4
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                ip = ip.MapToIPv4();

            return ip.ToString();
        }

        private string? GetUserAgent()
            => _httpContext?.HttpContext?.Request.Headers["User-Agent"].ToString();


        private static void ValidatePaging(int pageNumber, int pageSize)
        {
            if (pageNumber < 1)
                throw new ArgumentOutOfRangeException(nameof(pageNumber),
                    "Page number must be greater than zero.");
            if (pageSize < 1)
                throw new ArgumentOutOfRangeException(nameof(pageSize),
                    "Page size must be greater than zero.");
        }
    }
}