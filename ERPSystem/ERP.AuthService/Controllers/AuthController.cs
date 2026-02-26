using ERP.AuthService.Application.DTOs.AuthUser;
using ERP.AuthService.Application.Exceptions.AuthUser;
using ERP.AuthService.Application.Interfaces.Services;
using ERP.AuthService.Properties;
using Microsoft.AspNetCore.Mvc;
using System.Security;

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
            var result = await _authService.GetByIdAsync(id);
            return Ok(result);
        }


        [HttpGet("{login:string}")]
        [ProducesResponseType(typeof(AuthUserGetResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAuthUserByLogin(string login)
        {
            var result = await _authService.GetByLoginAsync(login);
            return Ok(result);
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
