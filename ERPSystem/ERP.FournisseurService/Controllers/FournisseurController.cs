using ERP.FournisseurService.Application.DTOs;
using ERP.FournisseurService.Application.Interfaces;
using ERP.FournisseurService.Properties;
using Microsoft.AspNetCore.Mvc;

namespace ERP.FournisseurService.Controllers;

[ApiController]
public class FournisseurController : ControllerBase
{
    private readonly IFournisseurService _service;

    public FournisseurController(IFournisseurService service) => _service = service;

    // =========================
    // READ
    // =========================

    [HttpGet(ApiRoutes.Fournisseurs.GetAll)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int size = 10)
    {
        var result = await _service.GetAllAsync(page, size);
        return Ok(result);
    }

    [HttpGet(ApiRoutes.Fournisseurs.GetById)]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpGet(ApiRoutes.Fournisseurs.GetDeleted)]
    public async Task<IActionResult> GetDeleted([FromQuery] int page = 1, [FromQuery] int size = 10)
    {
        var result = await _service.GetPagedDeletedAsync(page, size);
        return Ok(result);
    }

    [HttpGet(ApiRoutes.Fournisseurs.GetByName)]
    public async Task<IActionResult> GetByName(
        [FromQuery] string name,
        [FromQuery] int page = 1,
        [FromQuery] int size = 10)
    {
        var result = await _service.GetPagedByNameAsync(name, page, size);
        return Ok(result);
    }

    [HttpGet(ApiRoutes.Fournisseurs.GetStats)]
    public async Task<IActionResult> GetStats()
    {
        var result = await _service.GetStatsAsync();
        return Ok(result);
    }

    // =========================
    // CREATE / UPDATE
    // =========================

    [HttpPost(ApiRoutes.Fournisseurs.Create)]
    public async Task<IActionResult> Create([FromBody] CreateFournisseurRequestDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut(ApiRoutes.Fournisseurs.Update)]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateFournisseurRequestDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        return Ok(result);
    }

    // =========================
    // DELETE / RESTORE
    // =========================

    [HttpDelete(ApiRoutes.Fournisseurs.Delete)]
    public async Task<IActionResult> Delete([FromRoute] Guid id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }

    [HttpPatch(ApiRoutes.Fournisseurs.Restore)]
    public async Task<IActionResult> Restore([FromRoute] Guid id)
    {
        await _service.RestoreAsync(id);
        return NoContent();
    }

    // =========================
    // BLOCK / UNBLOCK
    // =========================

    [HttpPatch(ApiRoutes.Fournisseurs.Block)]
    public async Task<IActionResult> Block([FromRoute] Guid id)
    {
        var result = await _service.BlockAsync(id);
        return Ok(result);
    }

    [HttpPatch(ApiRoutes.Fournisseurs.Unblock)]
    public async Task<IActionResult> Unblock([FromRoute] Guid id)
    {
        var result = await _service.UnblockAsync(id);
        return Ok(result);
    }
}