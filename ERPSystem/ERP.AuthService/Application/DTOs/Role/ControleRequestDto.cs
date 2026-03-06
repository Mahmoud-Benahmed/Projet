namespace ERP.AuthService.Application.DTOs.Role
{
    public record ControleRequestDto(
        string Category,
        string Libelle,
        string Description
    );
}
