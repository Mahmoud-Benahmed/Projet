using ERP.AuthService.Application.DTOs;
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


        [HttpGet("me")]
        [ProducesResponseType(typeof(AuthUserGetResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMe()
        {
            // Extract authUserId from the JWT claim
            var id = User.FindFirstValue("sub")
                          ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(id, out var authUserIdGuid))
                return Unauthorized();

            var user = await _authService.GetByIdAsync(authUserIdGuid);

            if (user == null)
                return NotFound();

            return Ok(user);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(AuthUserGetResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            var role = User.FindFirstValue("role");

            if (!Guid.TryParse(sub, out var requesterId))
                throw new UnauthorizedAccessException("Invalid token.");

            var user = await _authService.GetByIdAsync(id);
            bool isSelf = requesterId == id;
            if (isSelf && !user.IsActive)
                Forbid();

            bool isAdmin = role == RoleEnum.SystemAdmin.ToString();

            if (!isSelf && !isAdmin)
                throw new UnauthorizedAccessException("You are not authorized to access this resource.");

            return Ok(user);
        }


        [HttpGet("login/{login}")]
        [ProducesResponseType(typeof(AuthUserGetResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetByLogin(string login)
        {
            var result = await _authService.GetByLoginAsync(login);
            return Ok(result);
        }


        [HttpGet]
        [ProducesResponseType(typeof(PagedResultDto<AuthUserGetResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var currentUserId = User.FindFirstValue("sub");
            Guid.TryParse(currentUserId, out var excludeId);

            var result = await _authService.GetAllAsync(pageNumber, pageSize, excludeId);
            return Ok(result);
        }

        [HttpGet("deactivated")]
        [ProducesResponseType(typeof(PagedResultDto<AuthUserGetResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDeactivated(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var currentUserId = User.FindFirstValue("sub");
            Guid.TryParse(currentUserId, out var excludeId);

            var result = await _authService.GetPagedByStatusAsync(false, pageNumber, pageSize, excludeId);
            return Ok(result);
        }

        [HttpGet("activated")]
        [ProducesResponseType(typeof(PagedResultDto<AuthUserGetResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetActivated(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var currentUserId = User.FindFirstValue("sub");
            Guid.TryParse(currentUserId, out var excludeId);

            var result = await _authService.GetPagedByStatusAsync(true, pageNumber, pageSize, excludeId);
            return Ok(result);
        }

        [HttpPatch("{id:guid}/activate")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Activate(Guid id)
        {
            var requesterIdString = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(requesterIdString) || !Guid.TryParse(requesterIdString, out var requesterId))
            {
                return Forbid();
            }
            await _authService.ActivateAsync(id, requesterId);
            return NoContent();
        }


        [HttpPatch("{id:guid}/deactivate")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Deactivate(Guid id)
        {
            var requesterIdString = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(requesterIdString) || !Guid.TryParse(requesterIdString, out var requesterId))
            {
                return Forbid();
            }
            await _authService.DeactivateAsync(id, requesterId);
            return NoContent();
        }


        [HttpDelete("delete/soft/{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DeleteSoft(Guid id)
        {
            // Extract authUserId from the JWT claim
            var requesterIdString = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(requesterIdString) || !Guid.TryParse(requesterIdString, out var requesterId))
            {
                return Forbid();
            }

            await _authService.SoftDeleteAsync(id, requesterId);
            return NoContent();
        }

        [HttpPatch("recover/{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Recover(Guid id)
        {
            // Extract authUserId from the JWT claim
            var requesterIdString = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(requesterIdString) || !Guid.TryParse(requesterIdString, out var requesterId))
            {
                return Forbid();
            }

            await _authService.RecoverAsync(id, requesterId);
            return NoContent();
        }

        [HttpGet("deleted")]
        [ProducesResponseType(typeof(PagedResultDto<AuthUserGetResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDeleted(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var currentUserId = User.FindFirstValue("sub");
            Guid.TryParse(currentUserId, out var excludeId);

            var result = await _authService.GetDeletedPagedAsync(pageNumber, pageSize, excludeId);
            return Ok(result);
        }

        [HttpDelete("delete/hard/{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Delete(Guid id)
        {
            // Extract authUserId from the JWT claim
            var requesterIdString = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(requesterIdString) || !Guid.TryParse(requesterIdString, out var requesterId))
            {
                return Forbid();
            }

            await _authService.DeleteAsync(id, requesterId);
            return NoContent();
        }



        [HttpGet("by-role")]
        [ProducesResponseType(typeof(PagedResultDto<AuthUserGetResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetByRole(
            [FromQuery] Guid roleId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var currentUserId = User.FindFirstValue("sub");
            Guid.TryParse(currentUserId, out var excludeId);

            var result = await _authService.GetPagedByRoleAsync(roleId, pageNumber, pageSize, excludeId);
            return Ok(result);
        }


        [HttpGet("exists-login/{login}")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<IActionResult> ExistsByLogin(string login)
        {
            var result = await _authService.ExistsByLogin(login);
            return Ok(result);
        }

        [HttpGet("exists-email/{email}")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<bool> ExistsByEmail(string email)
        {
            return await _authService.ExistsByEmail(email);
        }

        [HttpGet("stats")]
        [ProducesResponseType(typeof(UserStatsDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStats()
        {
            var currentUserId = User.FindFirstValue("sub");
            Guid.TryParse(currentUserId, out var excludeId);

            var result = await _authService.GetStatsAsync(excludeId);
            return Ok(result);
        }


        [HttpPost("register")]
        [ProducesResponseType(typeof(AuthUserGetResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> Register(RegisterRequestDto request)
        {
            var result = await _authService.RegisterAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpPut("update/{id:guid}")]
        [ProducesResponseType(typeof(AuthUserGetResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateProfile(Guid id, [FromBody] UpdateProfileDto updatedProfile)
        {
            var currentUserId = User.FindFirstValue("sub");
            var canManageUsers = User.HasClaim("privilege", "ManageUsers");
            var isOwner = currentUserId == id.ToString();

            if (!isOwner && !canManageUsers)
                return Forbid();

            var result = await _authService.UpdateProfile(id, updatedProfile);
            return Ok(result);
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequestDto request)
        {
            var result = await _authService.LoginAsync(request);
            return Ok(result);
        }


        [HttpPut("change-password/profile")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangeProfilePasswordRequestDto request)
        {
            var requesterId = User.FindFirst("sub")?.Value;

            if (requesterId is null || !Guid.TryParse(requesterId, out var id))
                return Unauthorized(new { message = "Invalid token." });

            await _authService.ChangeAuthPasswordAsync(
                    id,
                    request.CurrentPassword,
                    request.NewPassword);

                return NoContent();
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

            await _authService.ChangePasswordByAdminAsync(userId, request.NewPassword, adminId);
            return NoContent();
        }


        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(RefreshTokenRequestDto request)
        {
            
            var result = await _authService.RefreshTokenAsync(request.RefreshToken);
            return Ok(result);
        }


        [HttpPost("revoke")]
        public async Task<IActionResult> Revoke(RefreshTokenRequestDto request)
        {
            await _authService.RevokeRefreshTokenAsync(request.RefreshToken);
            return NoContent();
        }

    }
}
