using ERP.AuthService.Application.DTOs.AuditLog;
using ERP.AuthService.Domain.Logger;
using ERP.AuthService.Application.DTOs.AuthUser;
using ERP.AuthService.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace ERP.AuthService.Controllers
{
    [ApiController]
    [Route("auth/audit")]
    public class AuditLogController : ControllerBase
    {
        private readonly IAuditLogService _auditLogService;

        public AuditLogController(IAuditLogService auditLogService)
        {
            _auditLogService = auditLogService;
        }

        // GET auth/audit
        [HttpGet]
        [ProducesResponseType(typeof(PagedResultDto<AuditLogResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            var result = await _auditLogService.GetAllAsync(pageNumber, pageSize);
            return Ok(result);
        }

        // GET auth/audit/user/{userId}
        [HttpGet("user/{userId:guid}")]
        [ProducesResponseType(typeof(PagedResultDto<AuditLogResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetByUser(
            Guid userId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            var result = await _auditLogService.GetByUserAsync(userId, pageNumber, pageSize);
            return Ok(result);
        }

        // GET auth/audit/action/{action}
        [HttpGet("by-action/{action}")]
        [ProducesResponseType(typeof(PagedResultDto<AuditLogResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetByAction(
            string action,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            if (!Enum.TryParse<AuditAction>(action, ignoreCase: true, out var auditAction))
                return BadRequest(new { message = $"'{action}' is not a valid audit action." });

            var result = await _auditLogService.GetByActionAsync(auditAction, pageNumber, pageSize);
            return Ok(result);
        }

        // GET auth/audit/count
        [HttpGet("count")]
        [ProducesResponseType(typeof(long), StatusCodes.Status200OK)]
        public async Task<IActionResult> Count()
        {
            var count = await _auditLogService.CountAsync();
            return Ok(count);
        }

        // DELETE auth/audit (dev only)
        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Clear()
        {
            await _auditLogService.ClearAsync();
            return NoContent();
        }
    }
}