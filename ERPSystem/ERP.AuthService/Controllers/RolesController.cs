using ERP.AuthService.Application.DTOs.Role;
using ERP.AuthService.Application.Interfaces.Services;
using ERP.AuthService.Properties;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ERP.AuthService.Api.Controllers
{
    [ApiController]
    [Route(ApiRoutes.Roles.Base)]
    public class RolesController : ControllerBase
    {
        private readonly IRoleService _roleService;

        public RolesController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        [HttpGet("paged")]
        public async Task<IActionResult> GetAllPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
            => Ok(await _roleService.GetAllPagedAsync(pageNumber, pageSize));

        [HttpGet]
        public async Task<IActionResult> GetAll()=> Ok(await _roleService.GetAllAsync());

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var role = await _roleService.GetByIdAsync(id);
            return role is null ? NotFound() : Ok(role);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RoleCreateDto dto)
        {
            if (!TryGetRequesterId(out var requesterId))
                return Unauthorized();

            var created = await _roleService.CreateRole(dto, requesterId);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] RoleUpdateDto dto)
        {
            if (!TryGetRequesterId(out var requesterId))
                return Unauthorized();

            var result = await _roleService.UpdateAsync(id, dto, requesterId);
            return result is null ? NotFound() : Ok(result);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (!TryGetRequesterId(out var requesterId))
                return Unauthorized();

            await _roleService.DeleteAsync(id, requesterId);
            return NoContent();
        }

        private bool TryGetRequesterId(out Guid requesterId)
        {
            requesterId = Guid.Empty;
            var raw = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            return !string.IsNullOrWhiteSpace(raw) && Guid.TryParse(raw, out requesterId);
        }
    }
}