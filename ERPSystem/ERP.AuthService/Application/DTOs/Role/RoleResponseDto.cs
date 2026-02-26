using ERP.AuthService.Domain;

namespace ERP.AuthService.Application.DTOs.Role
{
    public record RoleResponseDto(
       Guid Id,
       RoleEnum Libelle
   );
}
