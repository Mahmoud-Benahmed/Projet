using ERP.PaymentService.Domain;
using System.ComponentModel.DataAnnotations;

namespace ERP.PaymentService.Application.DTO;

public sealed record PaymentSummaryDto(
    Guid PaymentId,
    string Number,
    decimal AmountAllocated,
    PaymentMethod Method,
    DateTime PaymentDate,
    string? ExternalReference,
    string? Notes,
    bool IsCancelled
);

public sealed record CreatePaymentDto(
    [Required]
    Guid ClientId,

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "TotalAmount must be greater than zero.")]
    decimal TotalAmount,

    [Required]
    [EnumDataType(typeof(PaymentMethod), ErrorMessage = "Invalid payment method.")]
    PaymentMethod Method, 

    [Required]
    [DataType(DataType.DateTime)]
    DateTime PaymentDate,

    [MaxLength(100, ErrorMessage = "ExternalReference cannot exceed 100 characters.")]
    string? ExternalReference,

    [MaxLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
    string? Notes,

    [Required]
    [MinLength(1, ErrorMessage = "At least one allocation is required.")]
    List<CreatePaymentAllocationDto> Allocations
);

public sealed record CreatePaymentAllocationDto(
    [Required]
    Guid InvoiceId,

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "AmountAllocated must be greater than zero.")]
    decimal AmountAllocated
);

public sealed record CorrectPaymentDto(
    [Required]
    [EnumDataType(typeof(PaymentMethod), ErrorMessage = "Invalid payment method.")]
    PaymentMethod Method,

    [MaxLength(100, ErrorMessage = "ExternalReference cannot exceed 100 characters.")]
    string? ExternalReference,

    [MaxLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
    string? Notes
);

// READ / GET
public sealed record PaymentDto(
    Guid Id,
    string Number,
    Guid ClientId,
    decimal TotalAmount,
    decimal RemainingAmount,
    string Method,
    DateTime PaymentDate,
    string? ExternalReference,
    string? Notes,
    bool IsCancelled,
    DateTime? CancelledAt,
    List<PaymentAllocationDto> Allocations
);

public sealed record PaymentAllocationDto(
    Guid Id,
    Guid InvoiceId,
    decimal AmountAllocated
);


public record CreateRefundDto(Guid ClientId, Guid InvoiceId);
public record CompleteRefundDto(string ExternalReference);

public record RefundRequestDto(
    Guid Id,
    Guid ClientId,
    Guid InvoiceId,
    string Status,
    List<RefundLineDto> Lines
);
public record RefundLineDto(Guid PaymentId, Guid PaymentAllocationId, decimal Amount);