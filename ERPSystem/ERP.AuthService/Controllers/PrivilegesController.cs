using ERP.AuthService.Application.Interfaces.Services;
using ERP.AuthService.Properties;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.AuthService.Api.Controllers
{
    [ApiController]
    [Route(ApiRoutes.Privileges.Base)]
    [Authorize]
    public class PrivilegesController : ControllerBase
    {
        private readonly IPrivilegeService _privilegeService;

        public PrivilegesController(IPrivilegeService privilegeService)
        {
            _privilegeService = privilegeService;
        }

        [HttpGet("{roleId:guid}")]
        public async Task<IActionResult> GetByRoleId(Guid roleId)
            => Ok(await _privilegeService.GetByRoleIdAsync(roleId));

        [HttpPut("{roleId:guid}/{controleId:guid}/allow")]
        public async Task<IActionResult> Allow(Guid roleId, Guid controleId)
        {
            await _privilegeService.AllowAsync(roleId, controleId);
            return NoContent();
        }

        [HttpPut("{roleId:guid}/{controleId:guid}/deny")]
        public async Task<IActionResult> Deny(Guid roleId, Guid controleId)
        {
            await _privilegeService.DenyAsync(roleId, controleId);
            return NoContent();
        }
    }
}