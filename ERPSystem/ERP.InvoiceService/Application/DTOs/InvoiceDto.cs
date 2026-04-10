using System.ComponentModel.DataAnnotations;

namespace InvoiceService.Application.DTOs;

// ════════════════════════════════════════════════════════════════════════════
// READ DTOs
// ════════════════════════════════════════════════════════════════════════════

public record InvoiceDto(
    [property: Required] Guid Id,
    [property: Required][property: MinLength(1)][property: MaxLength(50)] string InvoiceNumber,
    [property: Required] DateTime InvoiceDate,
    [property: Required] DateTime DueDate,
    [property: Range(0, double.MaxValue)] decimal TotalHT,
    [property: Range(0, double.MaxValue)] decimal TotalTVA,
    [property: Range(0, double.MaxValue)] decimal TotalTTC,
    [property: Required][property: MinLength(1)] string Status,
    [property: Required] Guid ClientId,
    [property: Required][property: MinLength(1)][property: MaxLength(200)] string ClientFullName,
    [property: Required][property: MinLength(1)][property: MaxLength(500)] string ClientAddress,
    [property: MaxLength(1000)] string? AdditionalNotes,
    [property: Required] DateTime CreatedAt,
    [property: Required] DateTime UpdatedAt,
    [property: Required] bool IsDeleted,
    [property: Required][property: MinLength(1)] List<InvoiceItemDto> Items
);

public record InvoiceItemDto(
    [property: Required] Guid Id,
    [property: Required] Guid ArticleId,
    [property: Required][property: MinLength(1)][property: MaxLength(200)] string ArticleName,
    [property: Required][property: MinLength(1)][property: MaxLength(50)] string ArticleBarCode,
    [property: Required][property: Range(1, int.MaxValue)] int Quantity,
    [property: Required][property: Range(0, double.MaxValue)] decimal UniPriceHT,
    [property: Required][property: Range(0, 1)] decimal TaxRate,
    [property: Required][property: Range(0, double.MaxValue)] decimal TotalHT,
    [property: Required][property: Range(0, double.MaxValue)] decimal TotalTTC
);

// ════════════════════════════════════════════════════════════════════════════
// CREATE / COMMAND DTOs
// ════════════════════════════════════════════════════════════════════════════

public record CreateInvoiceDto(
    [property: Required] DateTime InvoiceDate,
    [property: Required] DateTime DueDate,
    [property: Required] Guid ClientId,
    [property: MaxLength(1000)] string? AdditionalNotes,
    [property: Required][property: MinLength(1)] List<CreateInvoiceItemDto> Items
);

public record CreateInvoiceItemDto(
    [property: Required] Guid ArticleId,
    [property: Required][property: Range(1, int.MaxValue)] int Quantity,
    [property: Required][property: Range(0, double.MaxValue)] decimal UniPriceHT,
    [property: Required][property: Range(0, 1)] decimal TaxRate
);

public record AddInvoiceItemDto(
    [property: Required] Guid ArticleId,
    [property: Required][property: Range(1, int.MaxValue)] int Quantity,
    [property: Required][property: Range(0, double.MaxValue)] decimal UniPriceHT,
    [property: Required][property: Range(0, 1)] decimal TaxRate
);

// ════════════════════════════════════════════════════════════════════════════
// STATS DTOs
// ════════════════════════════════════════════════════════════════════════════

public record InvoiceStatsDto(
    [property: Range(0, int.MaxValue)] int TotalInvoices,
    [property: Range(0, int.MaxValue)] int DraftCount,
    [property: Range(0, int.MaxValue)] int UnpaidCount,
    [property: Range(0, int.MaxValue)] int PaidCount,
    [property: Range(0, int.MaxValue)] int CancelledCount,
    [property: Range(0, int.MaxValue)] int DeletedCount,
    [property: Range(0, int.MaxValue)] int OverdueCount,
    [property: Range(0, double.MaxValue)] decimal TotalRevenueHT,
    [property: Range(0, double.MaxValue)] decimal TotalRevenueTTC,
    [property: Range(0, double.MaxValue)] decimal TotalTVACollected,
    [property: Range(0, double.MaxValue)] decimal OutstandingHT,
    [property: Range(0, double.MaxValue)] decimal OutstandingTTC,
    [property: Range(0, double.MaxValue)] decimal OverdueHT,
    [property: Range(0, double.MaxValue)] decimal OverdueTTC,
    [property: Range(0, double.MaxValue)] decimal AverageInvoiceValueHT,
    [property: Range(0, double.MaxValue)] double AveragePaymentDays,
    IReadOnlyList<ClientRevenueDto> TopClients,
    IReadOnlyList<MonthlyStatsDto> MonthlyBreakdown
);

public record ClientRevenueDto(
    [property: Required] Guid ClientId,
    [property: Required][property: MinLength(1)][property: MaxLength(200)] string ClientFullName,
    [property: Range(0, int.MaxValue)] int InvoiceCount,
    [property: Range(0, double.MaxValue)] decimal RevenueTTC
);

public record MonthlyStatsDto(
    [property: Range(1, 9999)] int Year,
    [property: Range(1, 12)] int Month,
    [property: Range(0, int.MaxValue)] int IssuedCount,
    [property: Range(0, int.MaxValue)] int PaidCount,
    [property: Range(0, double.MaxValue)] decimal IssuedTTC,
    [property: Range(0, double.MaxValue)] decimal PaidTTC
);