using ERP.AuthService.Domain;

namespace ERP.AuthService.Application.DTOs.Role
{
    public record RoleResponseDto(
       Guid Id,
       RoleEnum Libelle
    );

    public record RoleCreateDto(
       RoleEnum Libelle
    );

    public record RoleUpdateDto(
       RoleEnum Libelle
    );

}
