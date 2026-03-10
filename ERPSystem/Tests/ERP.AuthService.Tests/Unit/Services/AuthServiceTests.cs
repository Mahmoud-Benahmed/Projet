using ERP.AuthService.Application.DTOs;
using ERP.AuthService.Application.DTOs.AuthUser;
using ERP.AuthService.Application.Exceptions.AuthUser;
using ERP.AuthService.Application.Interfaces;
using ERP.AuthService.Application.Interfaces.Repositories;
using ERP.AuthService.Application.Interfaces.Services;
using ERP.AuthService.Application.Services;
using ERP.AuthService.Domain;
using ERP.AuthService.Infrastructure.Security;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace ERP.AuthService.Tests.Unit.Services
{
    public class AuthUserServiceTests
    {
        private readonly Mock<IAuthUserRepository> _userRepoMock = new();
        private readonly Mock<IRefreshTokenRepository> _refreshRepoMock = new();
        private readonly Mock<IRoleRepository> _roleRepoMock = new();
        private readonly Mock<IJwtTokenGenerator> _jwtMock = new();
        private readonly Mock<IPasswordHasher<AuthUser>> _hasherMock = new();
        private readonly Mock<IAuditLogger> _auditMock = new();
        private readonly Mock<IHttpContextAccessor> _httpMock = new();
        private readonly Mock<IControleRepository> _controleRepoMock = new();
        private readonly Mock<IPrivilegeRepository> _privilegeRepoMock= new();
        private readonly AuthUserService _service;

        private readonly Role _role = new Role(RoleEnum.SystemAdmin);

        public AuthUserServiceTests()
        {
            _privilegeRepoMock
                .Setup(p => p.GetByRoleIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<Privilege>());

            _controleRepoMock
                .Setup(c => c.GetAllAsync())
                .ReturnsAsync(new List<Controle>());

            _service = new AuthUserService(
                _auditMock.Object,
                _httpMock.Object,
                _userRepoMock.Object,
                _roleRepoMock.Object,
                _refreshRepoMock.Object,
                _jwtMock.Object,
                _hasherMock.Object,
                _controleRepoMock.Object,
                _privilegeRepoMock.Object);
        }

        private AuthUser MakeUser(string login = "john_doe")
        {
            var user = new AuthUser(login, "john@example.com", "John Doe");
            user.SetRole(_role.Id);
            user.SetPasswordHash("hashed_password");
            return user;
        }

        private AuthUserGetResponseDto MakeDto(AuthUser user)
            => new(user.Id, user.Email, user.Login, user.FullName,
                   user.RoleId, _role.Libelle.ToString(), user.MustChangePassword,
                   user.IsActive, user.CreatedAt, user.UpdatedAt, user.LastLoginAt);

        // =========================
        // GET BY ID
        // =========================
        [Fact]
        public async Task GetByIdAsync_ExistingUser_ShouldReturnDto()
        {
            var user = MakeUser();
            _userRepoMock.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
            _roleRepoMock.Setup(r => r.GetByIdAsync(user.RoleId)).ReturnsAsync(_role);

            var result = await _service.GetByIdAsync(user.Id);

            result.Should().NotBeNull();
            result.Login.Should().Be("john_doe");
            result.Email.Should().Be("john@example.com");
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingUser_ShouldThrowUserNotFoundException()
        {
            _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((AuthUser?)null);

            Func<Task> act = () => _service.GetByIdAsync(Guid.NewGuid());

            await act.Should().ThrowAsync<UserNotFoundException>();
        }

        // =========================
        // GET BY LOGIN
        // =========================
        [Fact]
        public async Task GetByLoginAsync_ExistingLogin_ShouldReturnDto()
        {
            var user = MakeUser();
            _userRepoMock.Setup(r => r.GetByLoginAsync("john_doe")).ReturnsAsync(user);
            _roleRepoMock.Setup(r => r.GetByIdAsync(user.RoleId)).ReturnsAsync(_role);

            var result = await _service.GetByLoginAsync("john_doe");

            result.Login.Should().Be("john_doe");
        }

        [Fact]
        public async Task GetByLoginAsync_NonExistingLogin_ShouldThrowUserNotFoundException()
        {
            _userRepoMock.Setup(r => r.GetByLoginAsync(It.IsAny<string>())).ReturnsAsync((AuthUser?)null);

            Func<Task> act = () => _service.GetByLoginAsync("unknown");

            await act.Should().ThrowAsync<UserNotFoundException>();
        }

        // =========================
        // EXISTS
        // =========================
        [Fact]
        public async Task ExistsByLogin_ExistingLogin_ShouldReturnTrue()
        {
            _userRepoMock.Setup(r => r.ExistsByLoginAsync("john_doe")).ReturnsAsync(true);

            var result = await _service.ExistsByLogin("john_doe");

            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsByLogin_NonExistingLogin_ShouldReturnFalse()
        {
            _userRepoMock.Setup(r => r.ExistsByLoginAsync("unknown")).ReturnsAsync(false);

            var result = await _service.ExistsByLogin("unknown");

            result.Should().BeFalse();
        }

        [Fact]
        public async Task ExistsByEmail_ExistingEmail_ShouldReturnTrue()
        {
            _userRepoMock.Setup(r => r.ExistsByEmailAsync("john@example.com")).ReturnsAsync(true);

            var result = await _service.ExistsByEmail("john@example.com");

            result.Should().BeTrue();
        }

        // =========================
        // ACTIVATE / DEACTIVATE
        // =========================
        [Fact]
        public async Task ActivateAsync_InactiveUser_ShouldActivate()
        {
            var user = MakeUser();
            user.Deactivate();
            _userRepoMock.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);

            await _service.ActivateAsync(user.Id);

            user.IsActive.Should().BeTrue();
            _userRepoMock.Verify(r => r.UpdateAsync(user), Times.Once);
        }

        [Fact]
        public async Task ActivateAsync_NonExistingUser_ShouldThrowUserNotFoundException()
        {
            _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((AuthUser?)null);

            Func<Task> act = () => _service.ActivateAsync(Guid.NewGuid());

            await act.Should().ThrowAsync<UserNotFoundException>();
        }

        [Fact]
        public async Task DeactivateAsync_ActiveUser_ShouldDeactivate()
        {
            var user = MakeUser();
            _userRepoMock.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);

            await _service.DeactivateAsync(user.Id);

            user.IsActive.Should().BeFalse();
            _userRepoMock.Verify(r => r.UpdateAsync(user), Times.Once);
        }

        [Fact]
        public async Task DeactivateAsync_NonExistingUser_ShouldThrowUserNotFoundException()
        {
            _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((AuthUser?)null);

            Func<Task> act = () => _service.DeactivateAsync(Guid.NewGuid());

            await act.Should().ThrowAsync<UserNotFoundException>();
        }

        // =========================
        // UPDATE PROFILE
        // =========================
        [Fact]
        public async Task UpdateProfile_ValidInputs_ShouldUpdateAndReturnDto()
        {
            var user = MakeUser();
            user.SetRole(_role.Id);

            _userRepoMock.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
            _roleRepoMock.Setup(r => r.GetByIdAsync(user.RoleId)).ReturnsAsync(_role);
            _privilegeRepoMock.Setup(p => p.GetByRoleIdAsync(user.RoleId))
                                .ReturnsAsync(new List<Privilege>());

            var request = new UpdateProfileDto("jane@example.com", "Jane Doe");
            var result = await _service.UpdateProfile(user.Id, request);

            result.Email.Should().Be("jane@example.com");
            result.FullName.Should().Be("Jane Doe");
            _userRepoMock.Verify(r => r.UpdateAsync(user), Times.Once);
        }

        [Fact]
        public async Task UpdateProfile_NonExistingUser_ShouldThrowUserNotFoundException()
        {
            _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((AuthUser?)null);

            Func<Task> act = () => _service.UpdateProfile(Guid.NewGuid(), new UpdateProfileDto("jane@example.com", "Jane Doe"));

            await act.Should().ThrowAsync<UserNotFoundException>();
        }

        // =========================
        // GET ALL
        // =========================
        [Fact]
        public async Task GetAllAsync_ShouldReturnPagedResult()
        {
            var user = MakeUser();
            var users = new List<AuthUser> { MakeUser("user1"), MakeUser("user2") };
            _userRepoMock.Setup(r => r.GetAllAsync(1, 10, user.Id)).Returns(Task.FromResult((users, 2)));
            _roleRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(_role);

            var result = await _service.GetAllAsync(1, 10, user.Id);

            result.Items.Should().HaveCount(2);
            result.TotalCount.Should().Be(2);
        }

        // =========================
        // GET STATS
        // =========================
        [Fact]
        public async Task GetStatsAsync_ShouldReturnCorrectStats()
        {
            _userRepoMock.Setup(r => r.CountAsync()).ReturnsAsync(10L);
            _userRepoMock.Setup(r => r.CountByStatusAsync(true)).ReturnsAsync(7L);
            _userRepoMock.Setup(r => r.CountByStatusAsync(false)).ReturnsAsync(3L);

            var result = await _service.GetStatsAsync();

            result.TotalUsers.Should().Be(10);
            result.ActiveUsers.Should().Be(7);
            result.DeactivatedUsers.Should().Be(3);
        }

        // =========================
        // LOGIN
        // =========================
        [Fact]
        public async Task LoginAsync_ValidCredentials_ShouldReturnAuthResponse()
        {
            var user = MakeUser();

            _userRepoMock.Setup(r => r.GetByLoginAsync("john_doe")).ReturnsAsync(user);
            _roleRepoMock.Setup(r => r.GetByIdAsync(user.RoleId)).ReturnsAsync(_role);

            _privilegeRepoMock.Setup(p => p.GetByRoleIdAsync(user.RoleId))
                        .ReturnsAsync(new List<Privilege>());

            _hasherMock.Setup(h => h.VerifyHashedPassword(user, user.PasswordHash, "Password1!"))
                       .Returns(PasswordVerificationResult.Success);

            _jwtMock.Setup(j => j.GenerateAccessToken(user.Id, user.Login, _role.Libelle, It.IsAny<IEnumerable<string>>()))
                    .Returns(("access_token", DateTime.UtcNow.AddHours(1)));
            _jwtMock.Setup(j => j.GenerateRefreshToken()).Returns("refresh_token");

            var result = await _service.LoginAsync(new LoginRequestDto("john_doe", "Password1!"));

            result.AccessToken.Should().Be("access_token");
            result.RefreshToken.Should().Be("refresh_token");
        }

        [Fact]
        public async Task LoginAsync_NonExistingUser_ShouldThrowInvalidCredentialsException()
        {
            _userRepoMock.Setup(r => r.GetByLoginAsync(It.IsAny<string>())).ReturnsAsync((AuthUser?)null);

            Func<Task> act = () => _service.LoginAsync(new LoginRequestDto("unknown", "Password1!"));

            await act.Should().ThrowAsync<InvalidCredentialsException>();
        }

        [Fact]
        public async Task LoginAsync_InactiveUser_ShouldThrowUserInactiveException()
        {
            var user = MakeUser();

            user.Deactivate();
            _userRepoMock.Setup(r => r.GetByLoginAsync("john_doe")).ReturnsAsync(user);

            Func<Task> act = () => _service.LoginAsync(new LoginRequestDto("john_doe", "Password1!"));

            await act.Should().ThrowAsync<UserInactiveException>();
        }

        [Fact]
        public async Task LoginAsync_WrongPassword_ShouldThrowInvalidCredentialsException()
        {
            var user = MakeUser();
            _userRepoMock.Setup(r => r.GetByLoginAsync("john_doe")).ReturnsAsync(user);
            _hasherMock.Setup(h => h.VerifyHashedPassword(user, user.PasswordHash, "WrongPassword1!"))
                       .Returns(PasswordVerificationResult.Failed);

            Func<Task> act = () => _service.LoginAsync(new LoginRequestDto("john_doe", "WrongPassword1!"));

            await act.Should().ThrowAsync<InvalidCredentialsException>();
        }
    }
}