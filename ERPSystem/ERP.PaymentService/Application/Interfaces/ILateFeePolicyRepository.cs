using ERP.PaymentService.Domain.Entities;

namespace ERP.PaymentService.Application.Interfaces
{
    public interface ILateFeePolicyRepository
    {
        Task<List<LateFeePolicy>> GetAllAsync();
        Task<LateFeePolicy?> GetByIdAsync(Guid id);
        Task<LateFeePolicy?> GetActivePolicyAsync();
        Task AddAsync(LateFeePolicy policy);
        Task UpdateAsync(LateFeePolicy policy);
        Task DeleteAsync(Guid id);
        Task SaveChangesAsync();
    }
}
