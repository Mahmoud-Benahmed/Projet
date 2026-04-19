using ERP.PaymentService.Domain.Entities;
using ERP.PaymentService.Domain.Enums;

namespace ERP.PaymentService.Application.Interfaces
{
    public interface IPaymentRepository
    {
        Task<List<Payment>> GetAllAsync();
        Task<Payment?> GetByIdAsync(Guid id);
        Task<List<Payment>> GetByInvoiceIdAsync(Guid invoiceId);
        Task<List<Payment>> GetByClientIdAsync(Guid clientId);
        Task<List<Payment>> GetByStatusAsync(PaymentStatus status);
        Task<List<Payment>> GetCompletedByInvoiceIdAsync(Guid invoiceId);
        Task AddAsync(Payment payment);
        Task UpdateAsync(Payment payment);
        Task SaveChangesAsync();
    }
}
