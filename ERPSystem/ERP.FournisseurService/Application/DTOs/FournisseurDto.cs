using ERP.FournisseurService.Domain;
using System.ComponentModel.DataAnnotations;

namespace ERP.FournisseurService.Application.DTOs;

// ── Fournisseur ───────────────────────────────────────────────────────────────
public record CreateFournisseurRequestDto(
    [Required] string Name,
    [Required] string Address,
    [Required] string Phone,
    [Required] string TaxNumber,
    [Required][Length(10, 50)] string RIB,
    string? Email = null);

public record UpdateFournisseurRequestDto(
    [Required] string Name,
    [Required] string Address,
    [Required] string Phone,
    [Required] string TaxNumber,
    [Required][Length(10, 50)] string RIB,
    string? Email = null);

public sealed record FournisseurResponseDto(
    Guid Id, string Name, string Address, string Phone,
    string? Email, string TaxNumber, string RIB,
    bool IsDeleted, bool IsBlocked,
    DateTime CreatedAt, DateTime? UpdatedAt);

public sealed record FournisseurStatsDto(
    int TotalFournisseurs, int ActiveFournisseurs,
    int BlockedFournisseurs, int DeletedFournisseurs
);

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
