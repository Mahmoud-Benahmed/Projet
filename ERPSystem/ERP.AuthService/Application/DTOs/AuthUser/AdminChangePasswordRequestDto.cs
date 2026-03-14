using System.ComponentModel.DataAnnotations;

namespace ERP.AuthService.Application.DTOs.AuthUser
{
    public record AdminChangeProfileRequest(
        [Required]
        [MinLength(8)]
        [MaxLength(128)]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*[0-9])(?=.*[^a-zA-Z0-9]).+$",
            ErrorMessage = "Password must contain at least one uppercase letter, one digit, and one special character.")]
        string NewPassword
        );
}
