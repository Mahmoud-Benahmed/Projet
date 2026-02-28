using ERP.AuthService.Application.DTOs.AuthUser;
using ERP.AuthService.Application.Exceptions.AuthUser;
using ERP.AuthService.Application.Interfaces.Services;
using ERP.AuthService.Domain;
using ERP.AuthService.Properties;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security;
using System.Security.Claims;

namespace ERP.AuthService.Controllers
{
    [ApiController]
    [Route(ApiRoutes.Auth.Base)]
    public class AuthController : ControllerBase
    {
        private readonly IAuthUserService _authService;

        public AuthController(IAuthUserService authService)
        {
            _authService = authService;
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(AuthUserGetResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAuthUserById(Guid id)
        {
            var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            var role = User.FindFirstValue("role");

            if (!Guid.TryParse(sub, out var requesterId))
                throw new UnauthorizedAccessException("Invalid token.");

            bool isSelf = requesterId == id;
            bool isAdmin = role == RoleEnum.SystemAdmin.ToString();

            if (!isSelf && !isAdmin)
                throw new UnauthorizedAccessException("You are not authorized to access this resource.");


            var result = await _authService.GetByIdAsync(id);
            return Ok(result);
        }


        [HttpGet("login/{login}")]
        [ProducesResponseType(typeof(AuthUserGetResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAuthUserByLogin(string login)
        {
            var result = await _authService.GetByLoginAsync(login);
            return Ok(result);
        }


        [HttpGet("exists-login/{login}")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<bool> ExistsByLogin(string login)
        {
            return await _authService.ExistsByLogin(login);
        }

        [HttpGet("exists-email/{email}")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<bool> ExistsByEmail(string email)
        {
            return await _authService.ExistsByEmail(email);
        }


        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequestDto request)
        {
            try
            {
                var result = await _authService.RegisterAsync(request);
                return Ok(result);
            }
            catch (EmailAlreadyExistsException ex)
            {
                ModelState.AddModelError("Email", ex.Message);
                return ValidationProblem(ModelState);
            }
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequestDto request)
        {
            try
            {
                var result = await _authService.LoginAsync(request);
                return Ok(result);
            }
            catch (InvalidCredentialsException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (UserInactiveException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { message = ex.Message });
            }
        }


        [HttpPut("change-password/profile")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request)
        {
            var requesterId = User.FindFirst("sub")?.Value;

            if (requesterId is null || !Guid.TryParse(requesterId, out var id))
                return Unauthorized(new { message = "Invalid token." });

            try
            {
                await _authService.ChangeAuthPasswordAsync(
                    id,
                    request.CurrentPassword,
                    request.NewPassword);

                return NoContent();
            }
            catch (UserNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidCredentialsException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        [HttpPut("change-password/{userId:guid}")]
        public async Task<IActionResult> AdminChangePassword(Guid userId, [FromBody] AdminChangeProfileRequest request)
        {
            var adminIdClaim = User.FindFirst("sub")?.Value;
            if (adminIdClaim is null || !Guid.TryParse(adminIdClaim, out var adminId))
                return Unauthorized(new { message = "Invalid token." });

            var roles = User.FindAll("role").Select(c => c.Value);
            if (!roles.Contains("SystemAdmin"))
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Only admins can change passwords." });

            try
            {
                await _authService.ChangePasswordByAdminAsync(userId, request.NewPassword, adminId);
                return NoContent();
            }
            catch (UserNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }


        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(RefreshTokenRequestDto request)
        {
            try
            {
                var result = await _authService.RefreshTokenAsync(request.RefreshToken);
                return Ok(result);
            }
            catch (InvalidCredentialsException)
            {
                return Unauthorized(new { message = "Invalid refresh token." });
            }
            catch (SecurityException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (TokenAlreadyRevokedException te)
            {
                return Unauthorized(new { message = te.Message });
            }
            catch (UnauthorizedAccessException e)
            {
                return Unauthorized(new { message = e.Message });
            }
        }


        [HttpPost("revoke")]
        public async Task<IActionResult> Revoke(RefreshTokenRequestDto request)
        {
            try
            {
                await _authService.RevokeRefreshTokenAsync(request.RefreshToken);
                return NoContent();
            }
            catch (TokenAlreadyRevokedException te)
            {
                return Unauthorized(new { message = te.Message });
            }
            catch (UnauthorizedAccessException e)
            {
                return Unauthorized(new { message = e.Message });
            }
        }

    }
}
