using ERP.PaymentService.Domain.LocalCache;

namespace ERP.PaymentService.Application.Interfaces
{
    public interface IInvoiceCacheRepository
    {
        Task<Invoice?> GetByIdAsync(Guid invoiceId);
        Task UpsertAsync(Invoice invoice);
        Task UpdateStatusAsync(Guid invoiceId, string status);
        Task UpdateTotalPaidAsync(Guid invoiceId, decimal totalPaid);
    }
}
