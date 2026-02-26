using System.ComponentModel.DataAnnotations;

namespace ERP.AuthService.Application.DTOs.AuthUser
{
    public record ChangePasswordRequestDto
    (
        [Required]
        [MinLength(8)]
        string CurrentPassword,

        [Required]
        [MinLength(8)]
        string NewPassword
    );
}