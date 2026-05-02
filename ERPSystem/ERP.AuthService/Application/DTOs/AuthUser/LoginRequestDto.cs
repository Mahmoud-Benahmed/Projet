using System.ComponentModel.DataAnnotations;

namespace ERP.AuthService.Application.DTOs.AuthUser
{
    public record LoginRequestDto(
        [Required]
        [MinLength(3)]
        [MaxLength(50)]
        [RegularExpression(RegexPatterns.UserLogin, ErrorMessage = "Login must contain only lowercase letters, digits, and underscores.")]
        string Login,

        [Required]
        [MinLength(8)]
        [MaxLength(128)]
        string Password
    );
}
