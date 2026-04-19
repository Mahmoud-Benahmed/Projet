using ERP.PaymentService.Application.Interfaces;
using ERP.PaymentService.Domain.LocalCache;
using Microsoft.EntityFrameworkCore;

namespace ERP.PaymentService.Infrastructure.Persistence.LocalCache.ClientCache
{
    public class ClientCacheRepository : IClientCacheRepository
    {
        private readonly PaymentDbContext _context;

        public ClientCacheRepository(PaymentDbContext context)
        {
            _context = context;
        }

        public async Task<Client?> GetByIdAsync(Guid clientId)
        {
            return await _context.ClientCache
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ClientId == clientId);
        }

        public async Task UpsertAsync(Client client)
        {
            var existing = await _context.ClientCache
                .FirstOrDefaultAsync(c => c.ClientId == client.ClientId);

            if (existing is null)
            {
                await _context.ClientCache.AddAsync(client);
            }
            else
            {
                existing.Name = client.Name;
                existing.DelaiRetour = client.DelaiRetour;
                existing.IsBlocked = client.IsBlocked;
                existing.IsDeleted = client.IsDeleted;

                _context.ClientCache.Update(existing);
            }

            await _context.SaveChangesAsync();
        }
    }
}
