using ERP.UserService.Application.DTOs;
using ERP.UserService.Application.Interfaces;
using ERP.UserService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.UserService.Controllers;

[ApiController]
[Route("users")]
[Produces("application/json")]
public class UserProfilesController : ControllerBase
{
    private readonly IUserProfileService _service;

    public UserProfilesController(IUserProfileService service)
    {
        _service = service;
    }

    // =========================
    // CREATE
    // =========================

    [HttpPost]
    [ProducesResponseType(typeof(UserProfileResponseDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateUserProfileDto dto)
    {
        var result = await _service.CreateProfileAsync(dto);

        return CreatedAtAction(nameof(GetById),new { id = result.Id },result);
    }



    // =========================
    // GET BY ID
    // =========================
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserProfileResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return Ok(result);
    }



    // =========================
    // GET BY AUTH USER ID
    // =========================
    [HttpGet("auth/{authUserId:guid}")]
    [ProducesResponseType(typeof(UserProfileResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByAuthUserId(Guid authUserId)
    {
        var result = await _service.GetByAuthUserIdAsync(authUserId);
        return Ok(result);
    }



    // =========================
    // GET ALL
    // =========================

    [HttpGet]
    [ProducesResponseType(typeof(List<UserProfileResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAllAsync();
        return Ok(result);
    }



    // =========================
    // COMPLETE PROFILE
    // =========================
    [HttpPut("{authUserId:guid}/complete")]
    [ProducesResponseType(typeof(UserProfileResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CompleteProfile(Guid authUserId, [FromBody] CompleteProfileDto dto)
    {
        var requesterId = User.FindFirst("sub")?.Value;

        if (requesterId is null || !Guid.TryParse(requesterId, out var id))
            return Unauthorized(new { message = "Invalid token." });

        // only the user himself or SystemAdmin can complete the profile
        if (id != authUserId && !User.IsInRole("SystemAdmin"))
            return Forbid();

        var result = await _service.CompleteProfileAsync(authUserId, dto);
        return Ok(result);
    }

    [HttpGet("active")]
    [ProducesResponseType(typeof(PagedResultDto<UserProfileResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveUsers(
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 10)
    {
        var result = await _service.GetPagedByStatusAsync(
            isActive: true,
            pageNumber,
            pageSize);

        return Ok(result);
    }

    // =========================
    // ACTIVATE
    // =========================

    [HttpPatch("{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Activate(Guid id)
    {
        await _service.ActivateAsync(id);
        return NoContent();
    }


    [HttpGet("deactivated")]
    [ProducesResponseType(typeof(PagedResultDto<UserProfileResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDeactivatedUsers(
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 10)
    {
        var result = await _service.GetPagedByStatusAsync(
            isActive: false,
            pageNumber,
            pageSize);

        return Ok(result);
    }

    // =========================
    // DEACTIVATE
    // =========================

    [HttpPatch("{id:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        await _service.DeactivateAsync(id);
        return NoContent();
    }

    // =========================
    // DELETE
    // =========================
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }


    // =========================
    // STATS
    // =========================
    [HttpGet("stats")]
    [ProducesResponseType(typeof(UserStatsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStats()
    {
        var result = await _service.GetStatsAsync();
        return Ok(result);
    }
}