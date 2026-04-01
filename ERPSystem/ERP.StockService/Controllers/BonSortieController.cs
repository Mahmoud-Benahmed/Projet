using ERP.StockService.Application.DTOs;
using ERP.StockService.Application.Interfaces;
using ERP.StockService.Properties;
using Microsoft.AspNetCore.Mvc;

namespace ERP.StockService.API.Controllers;

[ApiController]
public class BonSortieController : ControllerBase
{
    private readonly IBonSortieService _service;
    public BonSortieController(IBonSortieService service)=> _service = service;

    // =========================
    // READ
    // =========================
    [HttpGet(ApiRoutes.BonSorties.GetAll)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int size = 10)
    {
        var result = await _service.GetAllAsync(page, size);
        return Ok(result);
    }

    [HttpGet(ApiRoutes.BonSorties.GetById)]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpGet(ApiRoutes.BonSorties.GetDeleted)]
    public async Task<IActionResult> GetDeleted(
        [FromQuery] int page = 1,
        [FromQuery] int size = 10)
    {
        var result = await _service.GetPagedDeletedAsync(page, size);
        return Ok(result);
    }

    [HttpGet(ApiRoutes.BonSorties.GetByClient)]
    public async Task<IActionResult> GetByClient(
        [FromRoute] Guid clientId,
        [FromQuery] int page = 1,
        [FromQuery] int size = 10)
    {
        var result = await _service.GetPagedByClientAsync(clientId, page, size);
        return Ok(result);
    }

    [HttpGet(ApiRoutes.BonSorties.GetByDateRange)]
    public async Task<IActionResult> GetByDateRange(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] int page = 1,
        [FromQuery] int size = 10)
    {
        var result = await _service.GetPagedByDateRangeAsync(from, to, page, size);
        return Ok(result);
    }

    [HttpGet(ApiRoutes.BonSorties.GetStats)]
    public async Task<IActionResult> GetStats()
    {
        var result = await _service.GetStatsAsync();
        return Ok(result);
    }

    // =========================
    // CREATE / UPDATE / DELETE
    // =========================
    [HttpPost(ApiRoutes.BonSorties.Create)]
    public async Task<IActionResult> Create([FromBody] CreateBonSortieRequestDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            result);
    }

    [HttpPut(ApiRoutes.BonSorties.Update)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateBonSortieRequestDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        return Ok(result);
    }

    [HttpDelete(ApiRoutes.BonSorties.Delete)]
    public async Task<IActionResult> Delete([FromRoute] Guid id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}