using System.ComponentModel.DataAnnotations;

namespace ERP.AuthService.Application.DTOs
{
    public record UpdateProfileDto(
        [Required]
        [EmailAddress]
        [MaxLength(255)]
        string Email,

        [Required]
        [MaxLength(100)]
        [RegularExpression(RegexPatterns.FullName, ErrorMessage = "Full name contains invalid characters.")]
        string FullName
    );
}

