namespace ERP.InvoiceService.Application.DTOs;

// CLient DTOs
public sealed record ClientResponseDto(
    Guid Id,
    string Name,
    string Email,
    string Address,
    int DuePaymentPeriod,
    string? Phone,
    string? TaxNumber,
    decimal? CreditLimit,
    int? DelaiRetour,
    bool IsBlocked,
    bool IsDeleted,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    List<ClientCategoryResponseDto> Categories
);

public sealed record ClientCategoryResponseDto(
    Guid Id,
    string Name,
    string Code,
    int DelaiRetour,
    int DuePaymentPeriod,                   // ← added
    decimal? DiscountRate,
    decimal? CreditLimitMultiplier,
    bool UseBulkPricing,
    bool IsActive,
    bool IsDeleted,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);


// Article DTOs
public record CategoryResponseDto(
    Guid Id,
    string Name,
    decimal TVA,
    bool IsDeleted,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record ArticleResponseDto(
    Guid Id,
    CategoryResponseDto Category,
    string CodeRef,
    string BarCode,
    string Libelle,
    decimal Prix,
    string Unit,
    decimal TVA,
    bool IsDeleted,
    DateTime CreatedAt,
    DateTime? UpdatedAt
    );


public sealed record FournisseurResponseDto(
Guid Id, string Name, string Address, string Phone,
string? Email, string TaxNumber, string RIB,
bool IsDeleted, bool IsBlocked,
DateTime CreatedAt, DateTime? UpdatedAt);