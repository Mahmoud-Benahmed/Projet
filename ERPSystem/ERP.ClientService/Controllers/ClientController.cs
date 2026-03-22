using ERP.ClientService.API.Routes;
using ERP.ClientService.Application.DTOs;
using ERP.ClientService.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ERP.ClientService.API.Controllers;

[ApiController]
public class ClientController : ControllerBase
{
    private readonly IClientService _clientService;

    public ClientController(IClientService clientService)
    {
        _clientService = clientService;
    }

    [HttpGet(ApiRoutes.Clients.GetAll)]
    public async Task<ActionResult> GetAllAsync(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _clientService.GetAllAsync(pageNumber, pageSize);
        return Ok(new { result.Items, result.TotalCount });
    }

    [HttpGet(ApiRoutes.Clients.GetDeleted)]
    public async Task<ActionResult> GetDeletedAsync(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _clientService.GetPagedDeletedAsync(pageNumber, pageSize);
        return Ok(new { result.Items, result.TotalCount });
    }

    [HttpGet(ApiRoutes.Clients.GetById)]
    [ProducesResponseType(typeof(ClientResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetById([FromRoute] Guid id)
    {
        var client = await _clientService.GetByIdAsync(id);
        return Ok(client);
    }

    [HttpGet(ApiRoutes.Clients.GetByCategory)]
    public async Task<ActionResult> GetPagedByCategory(
        [FromQuery] Guid categoryId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _clientService
            .GetPagedByCategoryIdAsync(categoryId, pageNumber, pageSize);
        return Ok(new { result.Items, result.TotalCount });
    }

    [HttpGet(ApiRoutes.Clients.GetByName)]
    public async Task<ActionResult> GetPagedByName(
        [FromQuery] string nameFilter,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _clientService
            .GetPagedByNameAsync(nameFilter, pageNumber, pageSize);
        return Ok(new { result.Items, result.TotalCount });
    }

    [HttpGet(ApiRoutes.Clients.Stats)]
    [ProducesResponseType(typeof(ClientStatsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStats()
    {
        var result = await _clientService.GetStatsAsync();
        return Ok(result);
    }

    [HttpPost(ApiRoutes.Clients.Create)]
    [ProducesResponseType(typeof(ClientResponseDto), StatusCodes.Status201Created)]
    public async Task<ActionResult> Create([FromBody] CreateClientRequestDto request)
    {
        var client = await _clientService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = client.Id }, client);
    }

    [HttpPut(ApiRoutes.Clients.Update)]
    [ProducesResponseType(typeof(ClientResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateClientRequestDto request)
    {
        var client = await _clientService.UpdateAsync(id, request);
        return Ok(client);
    }

    [HttpDelete(ApiRoutes.Clients.Delete)]
    public async Task<ActionResult> Delete([FromRoute] Guid id)
    {
        await _clientService.DeleteAsync(id);
        return NoContent();
    }

    [HttpPatch(ApiRoutes.Clients.Restore)]
    public async Task<ActionResult> Restore([FromRoute] Guid id)
    {
        await _clientService.RestoreAsync(id);
        return NoContent();
    }

    [HttpPatch(ApiRoutes.Clients.Block)]
    [ProducesResponseType(typeof(ClientResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult> Block([FromRoute] Guid id)
    {
        var client = await _clientService.BlockAsync(id);
        return Ok(client);
    }

    [HttpPatch(ApiRoutes.Clients.Unblock)]
    [ProducesResponseType(typeof(ClientResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult> Unblock([FromRoute] Guid id)
    {
        var client = await _clientService.UnblockAsync(id);
        return Ok(client);
    }

    [HttpPut(ApiRoutes.Clients.SetCreditLimit)]
    [ProducesResponseType(typeof(ClientResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult> SetCreditLimit(
        [FromRoute] Guid id,
        [FromBody] SetCreditLimitRequestDto request)
    {
        var client = await _clientService.SetCreditLimitAsync(id, request.Limit);
        return Ok(client);
    }

    [HttpDelete(ApiRoutes.Clients.RemoveCreditLimit)]
    [ProducesResponseType(typeof(ClientResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult> RemoveCreditLimit([FromRoute] Guid id)
    {
        var client = await _clientService.RemoveCreditLimitAsync(id);
        return Ok(client);
    }

    [HttpPut(ApiRoutes.Clients.SetDelaiRetour)]
    [ProducesResponseType(typeof(ClientResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult> SetDelaiRetour(
        [FromRoute] Guid id,
        [FromBody] SetDelaiRetourRequestDto request)
    {
        var client = await _clientService.SetDelaiRetourAsync(id, request.Days);
        return Ok(client);
    }

    [HttpDelete(ApiRoutes.Clients.ClearDelaiRetour)]
    [ProducesResponseType(typeof(ClientResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult> ClearDelaiRetour([FromRoute] Guid id)
    {
        var client = await _clientService.ClearDelaiRetourAsync(id);
        return Ok(client);
    }

    [HttpGet(ApiRoutes.Clients.EffectiveDelaiRetour)]
    public async Task<ActionResult> GetEffectiveDelaiRetour([FromRoute] Guid id)
    {
        var days = await _clientService.GetEffectiveDelaiRetourAsync(id);
        return Ok(new { effectiveDays = days });
    }

    [HttpGet(ApiRoutes.Clients.CanPlaceOrder)]
    public async Task<ActionResult> CanPlaceOrder(
        [FromRoute] Guid id,
        [FromQuery] decimal orderAmount,
        [FromQuery] decimal currentBalance)
    {
        var result = await _clientService
            .CanPlaceOrderAsync(id, orderAmount, currentBalance);
        return Ok(new { canPlace = result });
    }

    [HttpPost(ApiRoutes.Clients.AddCategory)]
    [ProducesResponseType(typeof(ClientResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult> AddCategory(
        [FromRoute] Guid id,
        [FromBody] AddCategoryRequestDto request)
    {
        var client = await _clientService
            .AddCategoryAsync(id, request.CategoryId, request.AssignedById);
        return Ok(client);
    }

    [HttpDelete(ApiRoutes.Clients.RemoveCategory)]
    [ProducesResponseType(typeof(ClientResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult> RemoveCategory(
        [FromRoute] Guid id,
        [FromRoute] Guid categoryId)
    {
        var client = await _clientService.RemoveCategoryAsync(id, categoryId);
        return Ok(client);
    }
}