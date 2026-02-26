namespace ERP.AuthService.Application.DTOs.Role
{
    public record ControleResponseDto(
        Guid Id,
        string Category,
        string Libelle,
        string Description
    );
}
