using System.ComponentModel.DataAnnotations;

namespace ERP.AuthService.Application.DTOs.AuthUser
{
    public record RefreshTokenRequestDto(
        [Required]
        string RefreshToken
    );

}
