
using ERP.ClientService.Application.DTOs;
using ERP.ClientService.Application.Interfaces;
using ERP.ClientService.Properties;
using Microsoft.AspNetCore.Mvc;

namespace ERP.ClientService.Controllers;

[ApiController]
public class CategoryController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoryController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet(ApiRoutes.Categories.GetAllPaged)]
    public async Task<ActionResult> GetAllPagedAsync(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _categoryService.GetAllPagedAsync(pageNumber, pageSize);
        return Ok(new { items = result.Items, totalCount = result.TotalCount });
    }


    [HttpGet(ApiRoutes.Categories.GetAll)]
    public async Task<ActionResult> GetAllAsync()
    {
        var result = await _categoryService.GetAllAsync();
        return Ok(result);
    }

    [HttpGet(ApiRoutes.Categories.GetDeleted)]
    public async Task<ActionResult> GetDeletedAsync(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _categoryService.GetPagedDeletedAsync(pageNumber, pageSize);
        return Ok(new { items = result.Items, totalCount = result.TotalCount });
    }

    [HttpGet(ApiRoutes.Categories.GetById)]
    [ProducesResponseType(typeof(CategoryResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetById([FromRoute] Guid id)
    {
        var category = await _categoryService.GetByIdAsync(id);
        return Ok(category);
    }

    [HttpGet(ApiRoutes.Categories.GetByName)]
    public async Task<ActionResult> GetPagedByName(
        [FromQuery] string nameFilter,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _categoryService
            .GetPagedByNameAsync(nameFilter, pageNumber, pageSize);
        return Ok(new { items = result.Items, totalCount = result.TotalCount });
    }

    [HttpGet(ApiRoutes.Categories.Stats)]
    [ProducesResponseType(typeof(CategoryStatsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStats()
    {
        var result = await _categoryService.GetStatsAsync();
        return Ok(result);
    }

    [HttpPost(ApiRoutes.Categories.Create)]
    [ProducesResponseType(typeof(CategoryResponseDto), StatusCodes.Status201Created)]
    public async Task<ActionResult> Create([FromBody] CreateCategoryRequestDto request)
    {
        var category = await _categoryService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = category.Id }, category);
    }

    [HttpPut(ApiRoutes.Categories.Update)]
    [ProducesResponseType(typeof(CategoryResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateCategoryRequestDto request)
    {
        var category = await _categoryService.UpdateAsync(id, request);
        return Ok(category);
    }

    [HttpDelete(ApiRoutes.Categories.Delete)]
    public async Task<ActionResult> Delete([FromRoute] Guid id)
    {
        await _categoryService.DeleteAsync(id);
        return NoContent();
    }

    [HttpPatch(ApiRoutes.Categories.Restore)]
    public async Task<ActionResult> Restore([FromRoute] Guid id)
    {
        await _categoryService.RestoreAsync(id);
        return NoContent();
    }

    [HttpPatch(ApiRoutes.Categories.Activate)]
    [ProducesResponseType(typeof(CategoryResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult> Activate([FromRoute] Guid id)
    {
        var category = await _categoryService.ActivateAsync(id);
        return Ok(category);
    }

    [HttpPatch(ApiRoutes.Categories.Deactivate)]
    [ProducesResponseType(typeof(CategoryResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult> Deactivate([FromRoute] Guid id)
    {
        var category = await _categoryService.DeactivateAsync(id);
        return Ok(category);
    }
}