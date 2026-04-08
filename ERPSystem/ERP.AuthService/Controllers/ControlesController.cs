using ERP.AuthService.Application.DTOs.Role;
using ERP.AuthService.Application.Interfaces.Services;
using ERP.AuthService.Properties;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ERP.AuthService.Api.Controllers
{
    [ApiController]
    [Route(ApiRoutes.Controles.Base)]
    public class ControlesController : ControllerBase
    {
        private readonly IControleService _controleService;

        public ControlesController(IControleService controleService)
        {
            _controleService = controleService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _controleService.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("paged")]
        public async Task<IActionResult> GetAllPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _controleService.GetAllPagedAsync(pageNumber, pageSize);
            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var controle = await _controleService.GetByIdAsync(id);
            return controle is null ? NotFound() : Ok(controle);
        }

        [HttpGet("by-category")]
        public async Task<IActionResult> GetByCategory([FromQuery] string category, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _controleService.GetByCategoryAsync(category, pageNumber, pageSize);
            return result is null ? NotFound() : Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ControleRequestDto dto)
        {
            if (!TryGetRequesterId(out var requesterId))
                return Unauthorized();

            var created = await _controleService.CreateControleAsync(dto, requesterId);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] ControleRequestDto dto)
        {
            if (!TryGetRequesterId(out var requesterId))
                return Unauthorized();

            var result = await _controleService.UpdateControleAsync(id, dto, requesterId);
            return result is null ? NotFound() : Ok(result);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (!TryGetRequesterId(out var requesterId))
                return Unauthorized();

            await _controleService.DeleteByIdAsync(id, requesterId);
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