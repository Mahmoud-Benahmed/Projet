namespace ERP.StockService.Application.DTOs;

public sealed record ClientResponseDto(
    Guid Id,
    string Name,
    string Email,
    string Address,
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
    DateTime AssignedAt
);


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
    decimal TVA,
    bool IsDeleted,
    DateTime CreatedAt,
    DateTime? UpdatedAt
    );