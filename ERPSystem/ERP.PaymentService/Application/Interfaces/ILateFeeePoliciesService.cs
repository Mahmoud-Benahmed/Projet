using ERP.PaymentService.Application.DTOs.LateFeePolicy;

namespace ERP.PaymentService.Application.Interfaces
{
    public interface ILateFeeePoliciesService
    {
        Task<List<LateFeeePolicyDto>> GetAllAsync();
        Task<LateFeeePolicyDto> GetActiveAsync();
        Task<LateFeeePolicyDto> GetByIdAsync(Guid id);
        Task<LateFeeePolicyDto> CreateAsync(CreateLateFeePolicyDto dto);
        Task<LateFeeePolicyDto> UpdateAsync(Guid id, UpdateLateFeePolicyDto dto);
        Task ActivateAsync(Guid id);
        Task DeleteAsync(Guid id);
    }
}
