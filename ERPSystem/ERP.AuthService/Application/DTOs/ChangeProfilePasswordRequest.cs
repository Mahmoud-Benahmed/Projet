using System.ComponentModel.DataAnnotations;

namespace ERP.AuthService.Application.DTOs
{
    public record AdminChangeProfileRequest(
        [Required]
        [MinLength(8)]
        string NewPassword
        );
}
