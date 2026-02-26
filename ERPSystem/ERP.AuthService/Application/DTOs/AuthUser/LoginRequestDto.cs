using System.ComponentModel.DataAnnotations;

namespace ERP.AuthService.Application.DTOs.AuthUser
{
    public record LoginRequestDto(
        [Required]
        [MinLength(5)]
        [MaxLength(50)]
        string Login,

        [Required]
        [MinLength(8)]
        string Password
    );
}
