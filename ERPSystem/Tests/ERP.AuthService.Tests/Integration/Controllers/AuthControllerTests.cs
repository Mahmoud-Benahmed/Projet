using ERP.AuthService.Application.DTOs;
using ERP.AuthService.Application.DTOs.AuthUser;
using ERP.AuthService.Application.Exceptions.AuthUser;
using ERP.AuthService.Application.Interfaces.Services;
using ERP.AuthService.Controllers;
using ERP.AuthService.Domain;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace ERP.AuthService.Tests.Integration.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<IAuthUserService> _serviceMock = new();
        private readonly AuthController _controller;
        private readonly Guid _userId = Guid.NewGuid();
        private readonly Guid _roleId = Guid.NewGuid();

        public AuthControllerTests()
        {
            _controller = new AuthController(_serviceMock.Object);
            SetupUserContext(_userId, RoleEnum.SystemAdmin);
        }

        private void SetupUserContext(Guid userId, RoleEnum role)
        {
            var claims = new List<Claim>
            {
                new Claim("sub", userId.ToString()),
                new Claim("role", role.ToString())
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        private AuthUserGetResponseDto MakeDto(
            Guid? id = null,
            string login = "john_doe",
            bool isActive = true)
            => new(id ?? Guid.NewGuid(), "john@example.com", login,
                   "John Doe", _roleId, "Employee", false, isActive,
                   DateTime.UtcNow, DateTime.UtcNow, null);

        private AuthResponseDto MakeAuthResponse()
            => new("access_token", "refresh_token", false, DateTime.UtcNow.AddHours(1));

        // =========================
        // REGISTER
        // =========================
        [Fact]
        public async Task Register_ValidRequest_ShouldReturn200()
        {
            var dto = MakeDto();
            _serviceMock.Setup(s => s.RegisterAsync(It.IsAny<RegisterRequestDto>()))
                        .ReturnsAsync(dto);

            var request = new RegisterRequestDto("john_doe", "john@example.com", "John Doe", "Password1!", _roleId);
            var result = await _controller.Register(request);

            result.Should().BeOfType<OkObjectResult>();
        }

        // =========================
        // LOGIN
        // =========================
        [Fact]
        public async Task Login_ValidCredentials_ShouldReturn200()
        {
            _serviceMock.Setup(s => s.LoginAsync(It.IsAny<LoginRequestDto>()))
                        .ReturnsAsync(MakeAuthResponse());

            var result = await _controller.Login(new LoginRequestDto("john_doe", "Password1!"));

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task Login_InvalidCredentials_ShouldReturn401()
        {
            _serviceMock.Setup(s => s.LoginAsync(It.IsAny<LoginRequestDto>()))
                        .ThrowsAsync(new InvalidCredentialsException("Invalid credentials"));

            var result = await _controller.Login(new LoginRequestDto("john_doe", "WrongPass1!"));

            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task Login_InactiveUser_ShouldReturn403()
        {
            _serviceMock.Setup(s => s.LoginAsync(It.IsAny<LoginRequestDto>()))
                        .ThrowsAsync(new UserInactiveException("User inactive"));

            var result = await _controller.Login(new LoginRequestDto("john_doe", "Password1!"));

            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(403);
        }

        // =========================
        // GET BY ID
        // =========================
        [Fact]
        public async Task GetById_ExistingUser_ShouldReturn200()
        {
            var dto = MakeDto(id: _userId);
            _serviceMock.Setup(s => s.GetByIdAsync(_userId)).ReturnsAsync(dto);

            var result = await _controller.GetById(_userId);

            result.Should().BeOfType<OkObjectResult>();
        }

        // =========================
        // GET BY LOGIN
        // =========================
        [Fact]
        public async Task GetByLogin_ExistingLogin_ShouldReturn200()
        {
            var dto = MakeDto();
            _serviceMock.Setup(s => s.GetByLoginAsync("john_doe")).ReturnsAsync(dto);

            var result = await _controller.GetByLogin("john_doe");

            result.Should().BeOfType<OkObjectResult>();
        }

        // =========================
        // GET ALL
        // =========================
        [Fact]
        public async Task GetAll_ShouldReturn200WithPagedResult()
        {
            var paged = new PagedResultDto<AuthUserGetResponseDto>(
                new List<AuthUserGetResponseDto> { MakeDto() }, 1, 1, 10);
            _serviceMock.Setup(s => s.GetAllAsync(1, 10, _userId)).ReturnsAsync(paged);

            var result = await _controller.GetAll(1, 10);

            result.Should().BeOfType<OkObjectResult>();
        }

        // =========================
        // ACTIVATE / DEACTIVATE
        // =========================
        [Fact]
        public async Task Activate_ExistingUser_ShouldReturn204()
        {
            var id = Guid.NewGuid();
            _serviceMock.Setup(s => s.ActivateAsync(id)).Returns(Task.CompletedTask);

            var result = await _controller.Activate(id);

            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task Activate_NonExistingUser_ShouldReturn404()
        {
            _serviceMock.Setup(s => s.ActivateAsync(It.IsAny<Guid>()))
                        .ThrowsAsync(new UserNotFoundException("Not found"));

            var result = await _controller.Activate(Guid.NewGuid());

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task Deactivate_ExistingUser_ShouldReturn204()
        {
            var id = Guid.NewGuid();
            _serviceMock.Setup(s => s.DeactivateAsync(id)).Returns(Task.CompletedTask);

            var result = await _controller.Deactivate(id);

            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task Deactivate_NonExistingUser_ShouldReturn404()
        {
            _serviceMock.Setup(s => s.DeactivateAsync(It.IsAny<Guid>()))
                        .ThrowsAsync(new UserNotFoundException("Not found"));

            var result = await _controller.Deactivate(Guid.NewGuid());

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        // =========================
        // EXISTS
        // =========================
        [Fact]
        public async Task ExistsByLogin_ExistingLogin_ShouldReturnTrue()
        {
            _serviceMock.Setup(s => s.ExistsByLogin("john_doe")).ReturnsAsync(true);

            var result = await _controller.ExistsByLogin("john_doe");

            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsByEmail_ExistingEmail_ShouldReturnTrue()
        {
            _serviceMock.Setup(s => s.ExistsByEmail("john@example.com")).ReturnsAsync(true);

            var result = await _controller.ExistsByEmail("john@example.com");

            result.Should().BeTrue();
        }

        // =========================
        // GET STATS
        // =========================
        [Fact]
        public async Task GetStats_ShouldReturn200WithStats()
        {
            var stats = new UserStatsDto { TotalUsers = 10, ActiveUsers = 7, DeactivatedUsers = 3 };
            _serviceMock.Setup(s => s.GetStatsAsync()).ReturnsAsync(stats);

            var result = await _controller.GetStats();

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            var body = ok.Value.Should().BeAssignableTo<UserStatsDto>().Subject;
            body.TotalUsers.Should().Be(10);
        }

        // =========================
        // REFRESH
        // =========================
        [Fact]
        public async Task Refresh_ValidToken_ShouldReturn200()
        {
            _serviceMock.Setup(s => s.RefreshTokenAsync("valid_refresh_token"))
                        .ReturnsAsync(MakeAuthResponse());

            var result = await _controller.Refresh(new RefreshTokenRequestDto("valid_refresh_token"));

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task Refresh_InvalidToken_ShouldReturn401()
        {
            _serviceMock.Setup(s => s.RefreshTokenAsync(It.IsAny<string>()))
                        .ThrowsAsync(new InvalidCredentialsException("Invalid token"));

            var result = await _controller.Refresh(new RefreshTokenRequestDto("invalid_token"));

            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        // =========================
        // REVOKE
        // =========================
        [Fact]
        public async Task Revoke_ValidToken_ShouldReturn204()
        {
            _serviceMock.Setup(s => s.RevokeRefreshTokenAsync("valid_token")).Returns(Task.CompletedTask);

            var result = await _controller.Revoke(new RefreshTokenRequestDto("valid_token"));

            result.Should().BeOfType<NoContentResult>();
        }

        // =========================
        // UPDATE PROFILE
        // =========================
        [Fact]
        public async Task UpdateProfile_SelfUpdate_ShouldReturn200()
        {
            var dto = MakeDto(id: _userId);
            _serviceMock.Setup(s => s.UpdateProfile(_userId, It.IsAny<UpdateProfileDto>()))
                        .ReturnsAsync(dto);

            var result = await _controller.UpdateProfile(_userId, new UpdateProfileDto("john@example.com", "John Doe"));

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task UpdateProfile_OtherUserWithoutPrivilege_ShouldReturn403()
        {
            SetupUserContext(_userId, RoleEnum.Accountant);
            var otherId = Guid.NewGuid();

            var result = await _controller.UpdateProfile(otherId, new UpdateProfileDto("other@example.com", "Other User"));

            result.Should().BeOfType<ForbidResult>();
        }
    }
}