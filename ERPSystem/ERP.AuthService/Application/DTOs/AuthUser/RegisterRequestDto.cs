using System.ComponentModel.DataAnnotations;

namespace ERP.AuthService.Application.DTOs.AuthUser
{
    public record RegisterRequestDto(
        [Required]
        [MinLength(3)]
        [MaxLength(50)]
        [RegularExpression("^[a-zA-Z0-9_]+$", ErrorMessage = "Login must contain only lowercase letters, digits, and underscores.")]
        string Login,

        [Required]
        [EmailAddress]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage = "Invalid email format.")]
        [MaxLength(255)]
        string Email,


        [Required]
        [MinLength(5)]
        [RegularExpression(@"^\p{L}+(\s\p{L}+)*$",
            ErrorMessage = "Full name must contain alphabetic characters only.")]// allow All Unicode letters — includes a-zA-Z + José, Müller, Ñoño.. and spaces
        string FullName,

        [Required]
        [MinLength(8)]
        [MaxLength(128)]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*[0-9])(?=.*[^a-zA-Z0-9]).+$",
            ErrorMessage = "Password must contain at least one uppercase letter, one digit, and one special character.")]
        string Password,

        [Required]
        Guid RoleId
    );
}