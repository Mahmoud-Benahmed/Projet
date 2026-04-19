using ERP.PaymentService.Application.DTOs.LateFeePolicy;
using ERP.PaymentService.Application.Exceptions;
using ERP.PaymentService.Application.Interfaces;
using ERP.PaymentService.Properties;
using Microsoft.AspNetCore.Mvc;

namespace ERP.PaymentService.Controllers
{
    [ApiController]
    public class LateFeePoliciesController : ControllerBase
    {
        private readonly ILateFeeePoliciesService _lateFeePoliciesService;
        private readonly ILogger<LateFeePoliciesController> _logger;

        public LateFeePoliciesController(
            ILogger<LateFeePoliciesController> logger,
            ILateFeeePoliciesService lateFeePoliciesService)
        {
            _logger = logger;
            _lateFeePoliciesService = lateFeePoliciesService;
        }

        // GET ENDPOINTS
        [HttpGet(ApiRoutes.LateFeePolicies.GetAll)]
        public async Task<IActionResult> GetAllAsync()
        {
            var policies = await _lateFeePoliciesService.GetAllAsync();
            return Ok(policies);
        }

        [HttpGet(ApiRoutes.LateFeePolicies.GetActive)]
        public async Task<IActionResult> GetActiveAsync()
        {
            try
            {
                var policy = await _lateFeePoliciesService.GetActiveAsync();
                return Ok(policy);
            }
            catch (NoActiveLateFeePolicyException ex)
            {
                _logger.LogWarning(ex, "\n\nNo active late fee policy found\n\n");
                return NotFound(ex.Message);
            }
        }

        [HttpGet(ApiRoutes.LateFeePolicies.GetById)]
        public async Task<IActionResult> GetByIdAsync(Guid id)
        {
            try
            {
                var policy = await _lateFeePoliciesService.GetByIdAsync(id);
                return Ok(policy);
            }
            catch (LateFeePolicyNotFoundException ex)
            {
                _logger.LogWarning(ex, "\n\nLate fee policy {PolicyId} not found\n\n", id);
                return NotFound(ex.Message);
            }
        }

        // COMMAND ENDPOINTS
        [HttpPost(ApiRoutes.LateFeePolicies.Create)]
        public async Task<IActionResult> CreateAsync([FromBody] CreateLateFeePolicyDto dto)
        {
            var policy = await _lateFeePoliciesService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetByIdAsync), new { id = policy.Id }, policy);
        }

        [HttpPut(ApiRoutes.LateFeePolicies.Update)]
        public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] UpdateLateFeePolicyDto dto)
        {
            try
            {
                await _lateFeePoliciesService.UpdateAsync(id, dto);
                return NoContent();
            }
            catch (LateFeePolicyNotFoundException ex)
            {
                _logger.LogWarning(ex, "\n\nLate fee policy {PolicyId} not found for update\n\n", id);
                return NotFound(ex.Message);
            }
        }

        [HttpPut(ApiRoutes.LateFeePolicies.Activate)]
        public async Task<IActionResult> ActivateAsync(Guid id)
        {
            try
            {
                await _lateFeePoliciesService.ActivateAsync(id);
                return NoContent();
            }
            catch (LateFeePolicyNotFoundException ex)
            {
                _logger.LogWarning(ex, "\n\nLate fee policy {PolicyId} not found for activation\n\n", id);
                return NotFound(ex.Message);
            }
        }

        [HttpDelete(ApiRoutes.LateFeePolicies.Delete)]
        public async Task<IActionResult> DeleteAsync(Guid id)
        {
            try
            {
                await _lateFeePoliciesService.DeleteAsync(id);
                return NoContent();
            }
            catch (LateFeePolicyNotFoundException ex)
            {
                _logger.LogWarning(ex, "\n\nLate fee policy {PolicyId} not found for deletion\n\n", id);
                return NotFound(ex.Message);
            }
        }
    }
}