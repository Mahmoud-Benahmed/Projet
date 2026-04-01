using ERP.StockService.Domain;
using System.ComponentModel.DataAnnotations;

namespace ERP.StockService.Application.DTOs;

// ── Fournisseur ───────────────────────────────────────────────────────────────
public record CreateFournisseurRequestDto(
    [Required] string Name,
    [Required] string Address,
    [Required] string Phone,
    [Required] string TaxNumber,
    [Required] string RIB,
    string? Email = null);

public record UpdateFournisseurRequestDto(
    [Required] string Name,
    [Required] string Address,
    [Required] string Phone,
    [Required] string TaxNumber,
    [Required] string RIB,
    string? Email = null);

public sealed record FournisseurResponseDto(
    Guid Id, string Name, string Address, string Phone,
    string? Email, string TaxNumber, string RIB,
    bool IsDeleted, bool IsBlocked,
    DateTime CreatedAt, DateTime? UpdatedAt);

// ------------ Lignes ------------------------
public record LigneRequestDto(
    [Required] Guid ArticleId,
    [Required][Range(0.001, double.MaxValue)] decimal Quantity,
    [Required][Range(0, double.MaxValue)] decimal Price,
    string? Remarque = null);

public sealed record LigneResponseDto(
    Guid Id,
    Guid ArticleId,
    decimal Quantity,
    decimal Price,
    decimal Total,
    string? Remarque = null);

// ── BonEntre ──────────────────────────────────────────────────────────────────
public record CreateBonEntreRequestDto(
    [Required] string Numero,
    [Required] Guid FournisseurId,
    string? Observation = null,
    List<LigneRequestDto>? Lignes = null);

public record UpdateBonEntreRequestDto(
    [Required][MaxLength(50)] string Numero,
    [Required] Guid FournisseurId,
    string? Observation = null,
    List<LigneRequestDto>? Lignes = null);

public sealed record BonEntreResponseDto(
    Guid Id, Guid FournisseurId, string FournisseurName,
    string Numero, string? Observation,
    bool IsDeleted, DateTime CreatedAt, DateTime? UpdatedAt,
    List<LigneResponseDto> Lignes, decimal Total);

// ── BonSortie ─────────────────────────────────────────────────────────────────
public record CreateBonSortieRequestDto(
    [Required] string Numero,
    [Required] Guid ClientId,
    string? Observation = null,
    List<LigneRequestDto>? Lignes = null);

public record UpdateBonSortieRequestDto(
    [Required][MaxLength(50)] string Numero,
    [Required] Guid ClientId,
    string? Observation = null,
    List<LigneRequestDto>? Lignes = null);

public sealed record BonSortieResponseDto(
    Guid Id, Guid ClientId,
    string Numero, string? Observation,
    bool IsDeleted, DateTime CreatedAt, DateTime? UpdatedAt,
    List<LigneResponseDto> Lignes, decimal Total);

// ── BonRetour ─────────────────────────────────────────────────────────────────
public record CreateBonRetourRequestDto(
    [Required] string Numero,
    [Required] Guid SourceId,
    [Required] RetourSourceType SourceType,
    [Required] string Motif,
    string? Observation = null,
    List<LigneRequestDto>? Lignes = null);

public record UpdateBonRetourRequestDto(
    [Required][MaxLength(50)] string Numero,
    [Required][MaxLength(500)] string Motif,
    [Required] Guid SourceId,
    string? Observation = null,
    List<LigneRequestDto>? Lignes = null);

public sealed record BonRetourResponseDto(
    Guid Id, Guid SourceId, RetourSourceType SourceType,
    string Motif, string Numero, string? Observation,
    bool IsDeleted, DateTime CreatedAt, DateTime? UpdatedAt,
    List<LigneResponseDto> Lignes, decimal Total);

// ── Shared ────────────────────────────────────────────────────────────────────
public sealed record FournisseurStatsDto(
    int TotalFournisseurs,  int ActiveFournisseurs, 
    int BlockedFournisseurs,int DeletedFournisseurs
);

public sealed record BonStatsDto(
    int TotalCount, int ActiveCount, int DeletedCount
);

public class ErrorResponse
{
    public required string Code { get; set; }
    public required string Message { get; set; }
    public int StatusCode { get; set; }
}
public sealed class PagedResultDto<T>
{
    public List<T> Items { get; }
    public int TotalCount { get; }
    public int PageNumber { get; }
    public int PageSize { get; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    public PagedResultDto(List<T> items, int totalCount, int pageNumber, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }
}
public static class FournisseurMapping
{
    public static FournisseurResponseDto ToResponseDto(this Fournisseur fournisseur) =>
        new FournisseurResponseDto(
            fournisseur.Id,
            fournisseur.Name,
            fournisseur.Address,
            fournisseur.Phone,
            fournisseur.Email,
            fournisseur.TaxNumber,
            fournisseur.RIB,
            fournisseur.IsDeleted,
            fournisseur.IsBlocked,
            fournisseur.CreatedAt,
            fournisseur.UpdatedAt
        );
}

public static class BonEntreMapping
{
    public static BonEntreResponseDto ToResponseDto(this BonEntre bon) =>
        new(bon.Id, bon.FournisseurId, bon.Fournisseur?.Name ?? string.Empty,
            bon.Numero, bon.Observation, bon.IsDeleted, bon.CreatedAt, bon.UpdatedAt,
            bon.Lignes.Select(l => new LigneResponseDto(
                l.Id, l.ArticleId, l.Quantity, l.Price, l.CalculateTotalLigne()
            // Remarque omitted → defaults to null
            )).ToList(),
            bon.CalculateTotal());
}

public static class BonSortieMapping
{
    public static BonSortieResponseDto ToResponseDto(this BonSortie bon) =>
        new(bon.Id, bon.ClientId,
            bon.Numero, bon.Observation, bon.IsDeleted, bon.CreatedAt, bon.UpdatedAt,
            bon.Lignes.Select(l => new LigneResponseDto(
                l.Id, l.ArticleId, l.Quantity, l.Price, l.CalculateTotalLigne()
            )).ToList(),
            bon.CalculateTotal());
}

public static class BonRetourMapping
{
    public static BonRetourResponseDto ToResponseDto(this BonRetour bon) =>
        new(bon.Id, bon.SourceId, bon.SourceType, bon.Motif,
            bon.Numero, bon.Observation, bon.IsDeleted, bon.CreatedAt, bon.UpdatedAt,
            bon.Lignes.Select(l => new LigneResponseDto(
                l.Id, l.ArticleId, l.Quantity, l.Price, l.CalculateTotalLigne(), l.Remarque
            )).ToList(),
            bon.CalculateTotal());
}