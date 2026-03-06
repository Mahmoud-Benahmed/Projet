using System.ComponentModel.DataAnnotations;

namespace ERP.AuthService.Application.DTOs
{
    public record UpdateProfileDto(
        [Required]
        [EmailAddress]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage = "Invalid email format.")]
        [MaxLength(255)]
        string Email,



        [Required]
        [MinLength(5)]
        [RegularExpression(@"^\p{L}+(\s\p{L}+)*$",
            ErrorMessage = "Full name must contain alphabetic characters only.")]// allow All Unicode letters — includes a-zA-Z + José, Müller, Ñoño.. and spaces
        string FullName
    );
}
    
