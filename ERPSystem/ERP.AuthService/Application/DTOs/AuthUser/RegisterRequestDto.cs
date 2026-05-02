using System.ComponentModel.DataAnnotations;

namespace ERP.AuthService.Application.DTOs.AuthUser
{
    public record RegisterRequestDto(
        [Required]
        [MinLength(3)]
        [MaxLength(50)]
        [RegularExpression(RegexPatterns.UserLogin, ErrorMessage = "Login must contain only lowercase letters, digits, and underscores.")]
        string Login,

        [Required]
        [EmailAddress]
        [MaxLength(255)]
        string Email,


        [Required]
        [MinLength(5)]
        [RegularExpression(RegexPatterns.FullName,
            ErrorMessage = "Full name must contain alphabetic characters only.")]// allow All Unicode letters — includes a-zA-Z + José, Müller, Ñoño.. and spaces
        string FullName,

        [Required]
        [MinLength(8)]
        [MaxLength(128)]
        string Password,

        [Required]
        Guid RoleId
    );
}