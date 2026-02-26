using System.ComponentModel.DataAnnotations;

namespace ERP.AuthService.Application.DTOs.AuthUser
{
    public record LoginRequestDto(
        [Required]
        [EmailAddress]
        string Email,

        [Required]
        [MinLength(8)]
        string Password
    );
}
