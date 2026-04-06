using ERP.StockService.Application.DTOs;
using ERP.StockService.Application.Interfaces;
using ERP.StockService.Properties;
using Microsoft.AspNetCore.Mvc;

namespace ERP.StockService.API.Controllers;

[ApiController]
public class BonRetoursController : ControllerBase
{
    private readonly IBonRetourService _service;

    public BonRetoursController(IBonRetourService service)
    {
        _service = service;
    }

    // =========================
    // READ
    // =========================
    [HttpGet(ApiRoutes.BonRetours.GetAll)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int size = 10)
    {
        var result = await _service.GetAllAsync(page, size);
        return Ok(result);
    }

    [HttpGet(ApiRoutes.BonRetours.GetById)]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpGet(ApiRoutes.BonRetours.GetBySource)]
    public async Task<IActionResult> GetBySource(
        [FromRoute] Guid sourceId,
        [FromQuery] int page = 1,
        [FromQuery] int size = 10)
    {
        var result = await _service.GetPagedBySourceAsync(sourceId, page, size);
        return Ok(result);
    }

    [HttpGet(ApiRoutes.BonRetours.GetByDateRange)]
    public async Task<IActionResult> GetByDateRange(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] int page = 1,
        [FromQuery] int size = 10)
    {
        var result = await _service.GetPagedByDateRangeAsync(from, to, page, size);
        return Ok(result);
    }

    [HttpGet(ApiRoutes.BonRetours.GetStats)]
    public async Task<IActionResult> GetStats()
    {
        var result = await _service.GetStatsAsync();
        return Ok(result);
    }


    // =========================
    // CREATE / UPDATE / DELETE
    // =========================
    [HttpPost(ApiRoutes.BonRetours.Create)]
    public async Task<IActionResult> Create([FromBody] CreateBonRetourRequestDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            result);
    }

    [HttpPut(ApiRoutes.BonRetours.Update)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateBonRetourRequestDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        return Ok(result);
    }

    [HttpDelete(ApiRoutes.BonRetours.Delete)]
    public async Task<IActionResult> Delete([FromRoute] Guid id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}