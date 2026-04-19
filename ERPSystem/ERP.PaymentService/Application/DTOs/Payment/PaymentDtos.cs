using ERP.PaymentService.Domain.Enums;

namespace ERP.PaymentService.Application.DTOs.Payment
{
    public record CreatePaymentDto(
        Guid InvoiceId,
        decimal Amount,
        PaymentMethod Method,
        DateTime PaymentDate);

    public record UpdatePaymentDto(
        decimal Amount,
        PaymentMethod Method,
        DateTime PaymentDate);

    public record PaymentDto(
        Guid Id,
        Guid InvoiceId,
        Guid ClientId,
        decimal Amount,
        PaymentMethod Method,
        PaymentStatus Status,
        decimal LateFeeApplied,
        DateTime PaymentDate,
        DateTime CreatedAt);

    public record PaymentSummaryDto(
        Guid InvoiceId,
        decimal TotalTTC,
        decimal TotalPaid,
        decimal RemainingAmount,
        string InvoiceStatus,
        decimal LateFeeAmount,
        List<PaymentDto> Payments);

    public record PaymentStatsDto(
        int TotalPayments,
        int TotalCompleted,
        int TotalPending,
        int TotalFailed,
        decimal TotalRevenue);
}
