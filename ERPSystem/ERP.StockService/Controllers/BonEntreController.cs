using ERP.StockService.API.Routes;
using ERP.StockService.Application.DTOs;
using ERP.StockService.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ERP.StockService.API.Controllers;

[ApiController]
public class BonEntreController : ControllerBase
{
    private readonly IBonEntreService _service;
    public BonEntreController(IBonEntreService service) => _service = service;


    // =========================
    // READ
    // =========================
    [HttpGet(ApiRoutes.BonEntres.GetAll)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int size = 10)
    {
        var result = await _service.GetAllAsync(page, size);
        return Ok(result);
    }

    [HttpGet(ApiRoutes.BonEntres.GetById)]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpGet(ApiRoutes.BonEntres.GetDeleted)]
    public async Task<IActionResult> GetDeleted(
        [FromQuery] int page = 1,
        [FromQuery] int size = 10)
    {
        var result = await _service.GetPagedDeletedAsync(page, size);
        return Ok(result);
    }

    [HttpGet(ApiRoutes.BonEntres.GetByFournisseur)]
    public async Task<IActionResult> GetByFournisseur(
        [FromRoute] Guid fournisseurId,
        [FromQuery] int page = 1,
        [FromQuery] int size = 10)
    {
        var result = await _service.GetPagedByFournisseurAsync(fournisseurId, page, size);
        return Ok(result);
    }

    [HttpGet(ApiRoutes.BonEntres.GetByDateRange)]
    public async Task<IActionResult> GetByDateRange(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] int page = 1,
        [FromQuery] int size = 10)
    {
        var result = await _service.GetPagedByDateRangeAsync(from, to, page, size);
        return Ok(result);
    }

    // =========================
    // CREATE / UPDATE / DELETE
    // =========================
    [HttpPost(ApiRoutes.BonEntres.Create)]
    public async Task<IActionResult> Create([FromBody] CreateBonEntreRequestDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            result);
    }

    [HttpPut(ApiRoutes.BonEntres.Update)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateBonEntreRequestDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        return Ok(result);
    }

    [HttpDelete(ApiRoutes.BonEntres.Delete)]
    public async Task<IActionResult> Delete([FromRoute] Guid id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }

    // =========================
    // LIGNES
    // =========================
    [HttpPost(ApiRoutes.BonEntres.AddLigne)]
    public async Task<IActionResult> AddLigne(
        [FromRoute] Guid id,
        [FromBody] AddLigneRequestDto dto)
    {
        var result = await _service.AddLigneAsync(id, dto);
        return Ok(result);
    }

    [HttpPut(ApiRoutes.BonEntres.UpdateLigne)]
    public async Task<IActionResult> UpdateLigne(
        [FromRoute] Guid id,
        [FromRoute] Guid ligneId,
        [FromBody] AddLigneRequestDto dto)
    {
        var result = await _service.UpdateLigneAsync(id, ligneId, dto);
        return Ok(result);
    }

    [HttpDelete(ApiRoutes.BonEntres.RemoveLigne)]
    public async Task<IActionResult> RemoveLigne(
        [FromRoute] Guid id,
        [FromRoute] Guid ligneId)
    {
        var result = await _service.RemoveLigneAsync(id, ligneId);
        return Ok(result);
    }
}