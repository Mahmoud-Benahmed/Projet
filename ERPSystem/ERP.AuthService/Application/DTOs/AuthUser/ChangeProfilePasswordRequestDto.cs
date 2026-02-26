using System.ComponentModel.DataAnnotations;

namespace ERP.AuthService.Application.DTOs.AuthUser
{
    public record AdminChangeProfileRequest(
        [Required]
        [MinLength(8)]
        string NewPassword
        );
}
