using ERP.StockService.Application.DTOs;
using ERP.StockService.Application.Interfaces;
using ERP.StockService.Properties;
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
        PagedResultDto<BonEntreResponseDto> result = await _service.GetAllAsync(page, size);
        return Ok(result);
    }

    [HttpGet(ApiRoutes.BonEntres.GetById)]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        BonEntreResponseDto result = await _service.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpGet(ApiRoutes.BonEntres.GetByFournisseur)]
    public async Task<IActionResult> GetByFournisseur(
        [FromRoute] Guid fournisseurId,
        [FromQuery] int page = 1,
        [FromQuery] int size = 10)
    {
        PagedResultDto<BonEntreResponseDto> result = await _service.GetPagedByFournisseurAsync(fournisseurId, page, size);
        return Ok(result);
    }

    [HttpGet(ApiRoutes.BonEntres.GetByDateRange)]
    public async Task<IActionResult> GetByDateRange(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] int page = 1,
        [FromQuery] int size = 10)
    {
        PagedResultDto<BonEntreResponseDto> result = await _service.GetPagedByDateRangeAsync(from, to, page, size);
        return Ok(result);
    }

    // =========================
    // CREATE / UPDATE / DELETE
    // =========================
    [HttpPost(ApiRoutes.BonEntres.Create)]
    public async Task<IActionResult> Create([FromBody] CreateBonEntreRequestDto dto)
    {
        BonEntreResponseDto result = await _service.CreateAsync(dto);
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
        BonEntreResponseDto result = await _service.UpdateAsync(id, dto);
        return Ok(result);
    }

    [HttpDelete(ApiRoutes.BonEntres.Delete)]
    public async Task<IActionResult> Delete([FromRoute] Guid id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }


    [HttpGet(ApiRoutes.BonEntres.GetStats)]
    public async Task<IActionResult> GetStats()
    {
        BonStatsDto result = await _service.GetStatsAsync();
        return Ok(result);
    }

    private bool TryGetRequesterId(out Guid requesterId)
    {
        requesterId = Guid.Empty;
        string? raw = HttpContext.Request.Headers["X-User-Id"].FirstOrDefault();
        return !string.IsNullOrWhiteSpace(raw) && Guid.TryParse(raw, out requesterId);
    }
}