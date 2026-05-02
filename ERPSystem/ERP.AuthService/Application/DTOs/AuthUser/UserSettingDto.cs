using System.ComponentModel.DataAnnotations;

namespace ERP.AuthService.Application.DTOs.AuthUser;

public record UserSettingsRequestDto(
    [Required] string Theme,
    [Required] string Language
);

public record UserSettingsResponseDto(
    string Theme,
    string Language
);
