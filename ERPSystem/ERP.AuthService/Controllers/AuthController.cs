using ERP.AuthService.Application.DTOs;
using ERP.AuthService.Application.Exceptions;
using ERP.AuthService.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security;

namespace ERP.AuthService.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
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
        public async Task<IActionResult> Login(LoginRequest request)
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


        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(RefreshTokenRequest request)
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
            catch(TokenAlreadyRevokedException te)
            {
                return Unauthorized(new { message = te.Message });
            }
            catch(UnauthorizedAccessException e)
            {
                return Unauthorized(new { message = e.Message });
            }
        }


        [HttpPost("revoke")]
        public async Task<IActionResult> Revoke(RefreshTokenRequest request)
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
            catch(UnauthorizedAccessException e)
            {
                return Unauthorized(new { message = e.Message });
            }
        }
    }
}
