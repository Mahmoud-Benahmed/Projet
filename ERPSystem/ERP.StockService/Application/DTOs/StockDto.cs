using ERP.StockService.Domain;
using System.ComponentModel.DataAnnotations;

namespace ERP.StockService.Application.DTOs;

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
    [Required] Guid FournisseurId,
    string? Observation = null,
    List<LigneRequestDto>? Lignes = null);

public record UpdateBonEntreRequestDto(
    [Required] Guid FournisseurId,
    string? Observation = null,
    List<LigneRequestDto>? Lignes = null);

public sealed record BonEntreResponseDto(
    Guid Id, Guid FournisseurId,
    string numero, string? Observation,
    DateTime CreatedAt, DateTime? UpdatedAt,
    List<LigneResponseDto> Lignes, decimal Total);

// ── BonSortie ─────────────────────────────────────────────────────────────────
public record CreateBonSortieRequestDto(
    [Required] Guid ClientId,
    string? Observation = null,
    List<LigneRequestDto>? Lignes = null);

public record UpdateBonSortieRequestDto(
    [Required] Guid ClientId,
    string? Observation = null,
    List<LigneRequestDto>? Lignes = null);

public sealed record BonSortieResponseDto(
    Guid Id, Guid ClientId,
    string numero, string? Observation,
    DateTime CreatedAt, DateTime? UpdatedAt,
    List<LigneResponseDto> Lignes, decimal Total);

// ── BonRetour ─────────────────────────────────────────────────────────────────
public record CreateBonRetourRequestDto(
    [Required] Guid SourceId,
    [Required] RetourSourceType SourceType,
    [Required] string Motif,
    string? Observation = null,
    List<LigneRequestDto>? Lignes = null);

public record UpdateBonRetourRequestDto(
    [Required] Guid SourceId,
    [Required][MaxLength(500)] string Motif,
    string? Observation = null,
    List<LigneRequestDto>? Lignes = null);

public sealed record BonRetourResponseDto(
    Guid Id, Guid SourceId, RetourSourceType SourceType,
    string numero, string Motif, string? Observation,
    DateTime CreatedAt, DateTime? UpdatedAt,
    List<LigneResponseDto> Lignes, decimal Total);

// ── Shared ────────────────────────────────────────────────────────────────────

public sealed record BonStatsDto(
    int TotalCount
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
public static class BonEntreMapping
{
    public static BonEntreResponseDto ToResponseDto(this BonEntre bon) =>
        new(bon.Id, bon.FournisseurId,
            bon.Numero, bon.Observation, bon.CreatedAt, bon.UpdatedAt,
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
            bon.Numero, bon.Observation, bon.CreatedAt, bon.UpdatedAt,
            bon.Lignes.Select(l => new LigneResponseDto(
                l.Id, l.ArticleId, l.Quantity, l.Price, l.CalculateTotalLigne()
            )).ToList(),
            bon.CalculateTotal());
}

public static class BonRetourMapping
{
    public static BonRetourResponseDto ToResponseDto(this BonRetour bon) =>
        new(bon.Id, bon.SourceId, bon.SourceType, bon.Numero,
            bon.Motif, bon.Observation, bon.CreatedAt, bon.UpdatedAt,
            bon.Lignes.Select(l => new LigneResponseDto(
                l.Id, l.ArticleId, l.Quantity, l.Price, l.CalculateTotalLigne(), l.Remarque
            )).ToList(),
            bon.CalculateTotal());
}