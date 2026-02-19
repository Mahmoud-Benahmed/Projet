using ERP.AuthService.Domain;
using System.ComponentModel.DataAnnotations;

namespace ERP.AuthService.Application.DTOs
{
    public record LoginRequest(
        [Required]
        [EmailAddress]
        string Email,

        [Required]
        [MinLength(8)]
        string Password
    );
}
