using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace ERP.AuthService.Application.DTOs.AuthUser
{
    public record ChangePasswordRequestDto(
        string CurrentPassword,
        string NewPassword
    );

    public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequestDto>
    {
        public ChangePasswordRequestValidator()
        {
            RuleFor(x => x.CurrentPassword)
                .NotEmpty()
                .WithMessage("Current password is required.");

            RuleFor(x => x.NewPassword)
                .NotEmpty()
                .MinimumLength(8)
                    .WithMessage("Password must be at least 8 characters.")
                .MaximumLength(128)
                    .WithMessage("Password must be no more than 128 characters.")
                .Must(p => !IsAllSameCharacter(p))
                    .WithMessage("Password cannot consist of a single repeated character.")
                // ── Cross-field check ──────────────────────────────────────────
                .Must((request, newPassword) => newPassword != request.CurrentPassword)
                    .WithMessage("New password must differ from the current one.");
        }

        private static bool IsAllSameCharacter(string p) =>
            p.Length > 0 && p.All(c => c == p[0]);
    }
}