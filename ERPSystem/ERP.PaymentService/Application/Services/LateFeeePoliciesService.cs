using ERP.PaymentService.Application.DTOs.LateFeePolicy;
using ERP.PaymentService.Application.Exceptions;
using ERP.PaymentService.Application.Interfaces;
using ERP.PaymentService.Domain.Entities;

namespace ERP.PaymentService.Application.Services
{
    public class LateFeeePoliciesService : ILateFeeePoliciesService
    {
        private readonly ILateFeePolicyRepository _lateFeePolicyRepository;
        private readonly ILogger<LateFeeePoliciesService> _logger;

        public LateFeeePoliciesService(
            ILogger<LateFeeePoliciesService> logger,
            ILateFeePolicyRepository lateFeePolicyRepository)
        {
            _logger = logger;
            _lateFeePolicyRepository = lateFeePolicyRepository;
        }

        // ════════════════════════════════════════════════════════════════════════════
        // QUERY OPERATIONS
        // ════════════════════════════════════════════════════════════════════════════

        public async Task<List<LateFeeePolicyDto>> GetAllAsync()
        {
            var policies = await _lateFeePolicyRepository.GetAllAsync();
            return policies.Select(p => p.ToDto()).ToList();
        }

        public async Task<LateFeeePolicyDto> GetActiveAsync()
        {
            var policy = await _lateFeePolicyRepository.GetActivePolicyAsync()
                ?? throw new NoActiveLateFeePolicyException();

            return policy.ToDto();
        }

        public async Task<LateFeeePolicyDto> GetByIdAsync(Guid id)
        {
            var policy = await _lateFeePolicyRepository.GetByIdAsync(id)
                ?? throw new LateFeePolicyNotFoundException(id);

            return policy.ToDto();
        }

        // ════════════════════════════════════════════════════════════════════════════
        // COMMAND OPERATIONS
        // ════════════════════════════════════════════════════════════════════════════

        public async Task<LateFeeePolicyDto> CreateAsync(CreateLateFeePolicyDto dto)
        {
            _logger.LogInformation(
                "\n\nCreating late fee policy. FeePercentage={FeePercentage}, FeeType={FeeType}, GracePeriodDays={GracePeriodDays}\n\n",
                dto.FeePercentage, dto.FeeType, dto.GracePeriodDays);

            var policy = new LateFeePolicy(
                feePercentage: dto.FeePercentage,
                feeType: dto.FeeType,
                gracePeriodDays: dto.GracePeriodDays);

            await _lateFeePolicyRepository.AddAsync(policy);
            await _lateFeePolicyRepository.SaveChangesAsync();

            return policy.ToDto();
        }

        public async Task<LateFeeePolicyDto> UpdateAsync(Guid id, UpdateLateFeePolicyDto dto)
        {
            var policy = await _lateFeePolicyRepository.GetByIdAsync(id)
                ?? throw new LateFeePolicyNotFoundException(id);

            _logger.LogInformation(
                "\n\nUpdating late fee policy {PolicyId}. FeePercentage={FeePercentage}, FeeType={FeeType}, GracePeriodDays={GracePeriodDays}\n\n",
                id, dto.FeePercentage, dto.FeeType, dto.GracePeriodDays);

            policy.Update(dto.FeePercentage, dto.FeeType, dto.GracePeriodDays);

            await _lateFeePolicyRepository.UpdateAsync(policy);
            await _lateFeePolicyRepository.SaveChangesAsync();

            return policy.ToDto();
        }

        public async Task ActivateAsync(Guid id)
        {
            // ──── 1. Fetch policy by id ────
            var policy = await _lateFeePolicyRepository.GetByIdAsync(id)
                ?? throw new LateFeePolicyNotFoundException(id);

            // ──── 2. Deactivate currently active policy ────
            var currentlyActive = await _lateFeePolicyRepository.GetActivePolicyAsync();
            if (currentlyActive is not null && currentlyActive.Id != id)
            {
                _logger.LogInformation(
                    "\n\nDeactivating previously active policy {PolicyId}\n\n",
                    currentlyActive.Id);

                currentlyActive.Deactivate();
                await _lateFeePolicyRepository.UpdateAsync(currentlyActive);
            }

            // ──── 3. Activate new policy ────
            _logger.LogInformation("\n\nActivating late fee policy {PolicyId}\n\n", id);

            policy.Activate();
            await _lateFeePolicyRepository.UpdateAsync(policy);
            await _lateFeePolicyRepository.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var policy = await _lateFeePolicyRepository.GetByIdAsync(id)
                ?? throw new LateFeePolicyNotFoundException(id);

            _logger.LogInformation("\n\nDeleting late fee policy {PolicyId}\n\n", id);

            await _lateFeePolicyRepository.DeleteAsync(policy.Id);
            await _lateFeePolicyRepository.SaveChangesAsync();
        }
    }

    // ════════════════════════════════════════════════════════════════════════════
    // MAPPING EXTENSIONS
    // ════════════════════════════════════════════════════════════════════════════

    internal static class LateFeePolicyMappingExtensions
    {
        internal static LateFeeePolicyDto ToDto(this LateFeePolicy p) => new(
            Id: p.Id,
            FeePercentage: p.FeePercentage,
            FeeType: p.FeeType,
            GracePeriodDays: p.GracePeriodDays,
            IsActive: p.IsActive,
            CreatedAt: p.CreatedAt);
    }
}
