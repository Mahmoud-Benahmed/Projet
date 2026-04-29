using ERP.PaymentService.Application.DTO;
using ERP.PaymentService.Domain;

namespace ERP.PaymentService.Application.Interfaces;

public interface IPaymentService
{
    Task<PaymentDto?> GetByIdAsync(Guid id);
    Task<PaymentDto?> GetByNumberAsync(string number);
    Task<PagedResultDto<PaymentDto>> GetByClientIdAsync(Guid clientId, int pageNumber, int pageSize);
    Task<PagedResultDto<PaymentDto>> GetPagedAsync(int pageNumber, int pageSize, string? search = null);
    Task<List<PaymentSummaryDto>> GetSummaryByInvoiceIdAsync(Guid invoiceId);
    Task<PaymentDto> CreateAsync(CreatePaymentDto dto);
    Task<PaymentDto> CorrectDetailsAsync(Guid id, CorrectPaymentDto dto);
    Task CancelAsync(Guid id);
}
public interface IPaymentNumberGenerator
{
    Task<string> GenerateNextPaymentNumberAsync();
}

public interface IRefundService
{
    Task<Guid> CreateRefundAsync(
        Guid clientId,
        Guid invoiceId,
        CancellationToken ct = default);

    Task CompleteRefundAsync(
        Guid refundId,
        string externalReference,
        CancellationToken ct = default);

    Task<RefundRequest?> GetByIdAsync(
        Guid refundId,
        CancellationToken ct = default);
}