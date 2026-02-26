using System.ComponentModel.DataAnnotations;

namespace ERP.AuthService.Application.DTOs.AuthUser
{
    public record RegisterRequestDto(
        [Required]
        [MinLength(5)]
        [MaxLength(50)]
        string Login,

        [Required]
        [EmailAddress]
        string Email,

        [Required]
        [MinLength(8)]
        string Password,

        [Required]
        Guid RoleId
    );
}
