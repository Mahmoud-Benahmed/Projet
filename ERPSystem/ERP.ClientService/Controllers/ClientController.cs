using ERP.ClientService.Application.DTOs;
using ERP.ClientService.Application.Exceptions;
using ERP.ClientService.Application.Interfaces;
using ERP.ClientService.Domain;
using ERP.ClientService.Properties;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.ClientService.Controllers
{
    [ApiController]
    public class ClientController : ControllerBase
    {
        private readonly IClientService _clientService;

        public ClientController(IClientService clientService)
        {
            _clientService = clientService;
        }

        // =========================
        // GET ALL
        // =========================
        [HttpGet(ApiRoutes.Clients.GetAll)]
        public async Task<ActionResult> GetAll(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _clientService.GetAllAsync(pageNumber, pageSize);
            return Ok(new { result.Items, result.TotalCount });
        }

        // =========================
        // GET BY ID
        // =========================
        [HttpGet(ApiRoutes.Clients.GetById)]
        public async Task<ActionResult> GetById([FromRoute] Guid id)
        {
            var result = await _clientService.GetByIdAsync(id);
            return Ok(result);
        }

        // =========================
        // GET PAGED BY TYPE
        // =========================
        [HttpGet(ApiRoutes.Clients.GetPagedByType)]
        public async Task<ActionResult> GetPagedByType(
            [FromQuery] ClientType type,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _clientService.GetPagedByTypeAsync(type, pageNumber, pageSize);
            return Ok(new { result.Items, result.TotalCount });
        }

        // =========================
        // GET DELETED
        // =========================
        [HttpGet(ApiRoutes.Clients.GetPagedDeleted)]
        public async Task<ActionResult> GetPagedDeleted(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
         
            var result = await _clientService.GetPagedDeletedAsync(pageNumber, pageSize);
            return Ok(new { result.Items, result.TotalCount });
        }

        // =========================
        // STATS
        // =========================
        [HttpGet(ApiRoutes.Clients.Stats)]
        public async Task<ActionResult> GetStats()
        {
            var result = await _clientService.GetStatsAsync();
            return Ok(result);
        }

        // =========================
        // CREATE
        // =========================
        [HttpPost(ApiRoutes.Clients.Create)]
        public async Task<ActionResult> Create([FromBody] CreateClientRequestDto dto)
        {
            var result = await _clientService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        // =========================
        // UPDATE
        // =========================
        [HttpPut(ApiRoutes.Clients.Update)]
        public async Task<ActionResult> Update(
            [FromRoute] Guid id,
            [FromBody] UpdateClientRequestDto dto)
        {
        
            var result = await _clientService.UpdateAsync(id, dto);
            return Ok(result);
        }

        // =========================
        // DELETE
        // =========================
        [HttpDelete(ApiRoutes.Clients.Delete)]
        public async Task<ActionResult> Delete([FromRoute] Guid id)
        {
            await _clientService.DeleteAsync(id);
            return NoContent();
        }

        // =========================
        // RESTORE
        // =========================
        [HttpPatch(ApiRoutes.Clients.Restore)]
        public async Task<ActionResult> Restore([FromRoute] Guid id)
        {
            await _clientService.RestoreAsync(id);
            return NoContent();
        }
    }
}