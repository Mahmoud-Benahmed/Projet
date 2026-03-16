using System.ComponentModel.DataAnnotations;

namespace ERP.AuthService.Application.DTOs.AuthUser
{
    public record AdminChangeProfileRequest(
        [Required]
        [MinLength(8)]
        [MaxLength(128)]
        string NewPassword
        );
}
