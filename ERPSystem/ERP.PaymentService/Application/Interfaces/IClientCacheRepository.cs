using ERP.PaymentService.Domain.LocalCache;

namespace ERP.PaymentService.Application.Interfaces
{
    public interface IClientCacheRepository
    {
        Task<Client?> GetByIdAsync(Guid clientId);
        Task UpsertAsync(Client client);
    }
}
