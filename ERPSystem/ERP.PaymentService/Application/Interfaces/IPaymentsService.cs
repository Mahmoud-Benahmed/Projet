using ERP.PaymentService.Application.DTOs.Payment;
using ERP.PaymentService.Domain.Enums;

namespace ERP.PaymentService.Application.Interfaces
{
    public interface IPaymentsService
    {
        Task<List<PaymentDto>> GetAllAsync();
        Task<PaymentDto> GetByIdAsync(Guid id);
        Task<List<PaymentDto>> GetByInvoiceIdAsync(Guid invoiceId);
        Task<List<PaymentDto>> GetByClientIdAsync(Guid clientId);
        Task<List<PaymentDto>> GetByStatusAsync(PaymentStatus status);
        Task<PaymentStatsDto> GetStatsAsync();
        Task<PaymentDto> CreateAsync(CreatePaymentDto dto);
        Task<PaymentDto> UpdateAsync(Guid id, UpdatePaymentDto dto);
        Task DeleteAsync(Guid id);
        Task RestoreAsync(Guid id);
        Task<PaymentSummaryDto> GetPaymentSummaryAsync(Guid invoiceId);
    }
}
