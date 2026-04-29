namespace ERP.PaymentService.Application.DTO;

public sealed record InvoiceEventDto(
    Guid Id,
    string InvoiceNumber,
    decimal TotalTTC,
    decimal PaidAmount,
    decimal RemainingAmount,
    string Status,
    Guid ClientId
);

public sealed record InvoicePaidEvent(
    Guid InvoiceId,
    Guid PaymentId,
    decimal PaidAmount,
    DateTime PaidAt
);