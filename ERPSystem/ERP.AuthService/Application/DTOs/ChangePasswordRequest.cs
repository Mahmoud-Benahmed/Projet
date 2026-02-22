using System.ComponentModel.DataAnnotations;

namespace ERP.AuthService.Application.DTOs
{
    public record ChangePasswordRequest
    (
        [Required]
        string CurrentPassword,

        [Required]
        [MinLength(8)]
        string NewPassword
    );
}