using ERP.PaymentService.Application.Interfaces;
using ERP.PaymentService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP.PaymentService.Infrastructure.Persistence
{
    public class LateFeePolicyRepository : ILateFeePolicyRepository
    {
        private readonly PaymentDbContext _context;

        public LateFeePolicyRepository(PaymentDbContext context)
        {
            _context = context;
        }

        public async Task<List<LateFeePolicy>> GetAllAsync()
        {
            return await _context.LateFeePolicies
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<LateFeePolicy?> GetByIdAsync(Guid id)
        {
            return await _context.LateFeePolicies
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<LateFeePolicy?> GetActivePolicyAsync()
        {
            return await _context.LateFeePolicies
                .FirstOrDefaultAsync(p => p.IsActive);
        }

        public async Task AddAsync(LateFeePolicy policy)
        {
            await _context.LateFeePolicies.AddAsync(policy);
        }

        public async Task UpdateAsync(LateFeePolicy policy)
        {
            _context.LateFeePolicies.Update(policy);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(Guid id)
        {
            var policy = await _context.LateFeePolicies.FindAsync(id);
            if (policy is not null)
                _context.LateFeePolicies.Remove(policy);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
