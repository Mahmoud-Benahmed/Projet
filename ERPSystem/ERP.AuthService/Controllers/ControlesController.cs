using ERP.AuthService.Application.Interfaces.Services;
using ERP.AuthService.Properties;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.AuthService.Api.Controllers
{
    [ApiController]
    [Route(ApiRoutes.Controles.Base)]
    [Authorize]
    public class ControlesController : ControllerBase
    {
        private readonly IControleService _controleService;

        public ControlesController(IControleService controleService)
        {
            _controleService = controleService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
            => Ok(await _controleService.GetAllAsync());

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
            => Ok(await _controleService.GetByIdAsync(id));

        [HttpGet("category/{category}")]
        public async Task<IActionResult> GetByCategory(string category)
            => Ok(await _controleService.GetByCategoryAsync(category));
    }
}