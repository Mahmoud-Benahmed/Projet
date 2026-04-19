using ERP.PaymentService.Domain.LocalCache;

namespace ERP.PaymentService.Application.Interfaces
{
    public interface IInvoiceServiceHttpClient
    {
        Task<Invoice?> GetInvoiceAsync(Guid invoiceId);
    }
}
