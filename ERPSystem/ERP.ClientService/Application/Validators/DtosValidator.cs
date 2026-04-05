using ERP.ClientService.Application.DTOs;
using ERP.ClientService.Domain;
using FluentValidation;

namespace ERP.ClientService.Application.Validators;

// ══════════════════════════════════════════════════════════════════════════════
// CATEGORY VALIDATORS
// ══════════════════════════════════════════════════════════════════════════════

public class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequestDto>
{
    public CreateCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
                .WithMessage("Name is required.")
            .MaximumLength(200)
                .WithMessage("Name cannot exceed 200 characters.");

        RuleFor(x => x.Code)
            .NotEmpty()
                .WithMessage("Code is required.")
            .MaximumLength(50)
                .WithMessage("Code cannot exceed 50 characters.")
            .Matches(@"^[A-Za-z0-9_\-]+$")
                .WithMessage("Code can only contain letters, digits, hyphens and underscores.");

        RuleFor(x => x.DelaiRetour)
            .GreaterThan(0)
                .WithMessage("Return delay must be at least 1 day.");

        RuleFor(x => x.DiscountRate)
            .InclusiveBetween(0m, 1m)
                .WithMessage("Discount rate must be between 0 and 1 (0% – 100%).")
            .When(x => x.DiscountRate.HasValue);

        RuleFor(x => x.CreditLimitMultiplier)
            .GreaterThan(0m)
                .WithMessage("Credit limit multiplier must be positive.")
            .When(x => x.CreditLimitMultiplier.HasValue);
    }
}

public class UpdateCategoryRequestValidator : AbstractValidator<UpdateCategoryRequestDto>
{
    public UpdateCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
                .WithMessage("Name is required.")
            .MaximumLength(200)
                .WithMessage("Name cannot exceed 200 characters.");

        RuleFor(x => x.Code)
            .NotEmpty()
                .WithMessage("Code is required.")
            .MaximumLength(50)
                .WithMessage("Code cannot exceed 50 characters.")
            .Matches(@"^[A-Za-z0-9_\-]+$")
                .WithMessage("Code can only contain letters, digits, hyphens and underscores.");

        RuleFor(x => x.DelaiRetour)
            .GreaterThan(0)
                .WithMessage("Return delay must be at least 1 day.");

        RuleFor(x => x.DiscountRate)
            .InclusiveBetween(0m, 1m)
                .WithMessage("Discount rate must be between 0 and 1 (0% – 100%).")
            .When(x => x.DiscountRate.HasValue);

        RuleFor(x => x.CreditLimitMultiplier)
            .GreaterThan(0m)
                .WithMessage("Credit limit multiplier must be positive.")
            .When(x => x.CreditLimitMultiplier.HasValue);
    }
}

// ══════════════════════════════════════════════════════════════════════════════
// CLIENT VALIDATORS
// ══════════════════════════════════════════════════════════════════════════════

public class CreateClientRequestValidator : AbstractValidator<CreateClientRequestDto>
{
    public CreateClientRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
                .WithMessage("Name is required.")
            .MaximumLength(200)
                .WithMessage("Name cannot exceed 200 characters.");

        RuleFor(x => x.Email)
            .NotEmpty()
                .WithMessage("Email is required.")
            .EmailAddress()
                .WithMessage("Email is not valid.")
            .MaximumLength(200)
                .WithMessage("Email cannot exceed 200 characters.");

        RuleFor(x => x.Address)
            .NotEmpty()
                .WithMessage("Address is required.")
            .MaximumLength(500)
                .WithMessage("Address cannot exceed 500 characters.");

        RuleFor(x => x.Phone)
            .MaximumLength(20)
                .WithMessage("Phone cannot exceed 20 characters.")
            .When(x => x.Phone is not null);

        RuleFor(x => x.TaxNumber)
            .MaximumLength(50)
                .WithMessage("Tax number cannot exceed 50 characters.")
            .When(x => x.TaxNumber is not null);

        RuleFor(x => x.CreditLimit)
            .GreaterThan(0m)
                .WithMessage("Credit limit must be positive.")
            .When(x => x.CreditLimit.HasValue);

        RuleFor(x => x.DelaiRetour)
            .GreaterThan(0)
                .WithMessage("Return delay must be at least 1 day.")
            .When(x => x.DelaiRetour.HasValue);
    }
}

public class UpdateClientRequestValidator : AbstractValidator<UpdateClientRequestDto>
{
    public UpdateClientRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
                .WithMessage("Name is required.")
            .MaximumLength(200)
                .WithMessage("Name cannot exceed 200 characters.");

        RuleFor(x => x.Email)
            .NotEmpty()
                .WithMessage("Email is required.")
            .EmailAddress()
                .WithMessage("Email is not valid.")
            .MaximumLength(200)
                .WithMessage("Email cannot exceed 200 characters.");

        RuleFor(x => x.Address)
            .NotEmpty()
                .WithMessage("Address is required.")
            .MaximumLength(500)
                .WithMessage("Address cannot exceed 500 characters.");

        RuleFor(x => x.Phone)
            .MaximumLength(20)
                .WithMessage("Phone cannot exceed 20 characters.")
            .When(x => x.Phone is not null);

        RuleFor(x => x.TaxNumber)
            .MaximumLength(50)
                .WithMessage("Tax number cannot exceed 50 characters.")
            .When(x => x.TaxNumber is not null);

        RuleFor(x => x.CreditLimit)
            .GreaterThan(0m)
                .WithMessage("Credit limit must be positive.")
            .When(x => x.CreditLimit.HasValue);

        RuleFor(x => x.DelaiRetour)
            .GreaterThan(0)
                .WithMessage("Return delay must be at least 1 day.")
            .When(x => x.DelaiRetour.HasValue);
    }
}


public class AddCategoryRequestValidator : AbstractValidator<AddCategoryRequestDto>
{
    public AddCategoryRequestValidator()
    {
        RuleFor(x => x.CategoryId)
            .NotEmpty()
                .WithMessage("CategoryId is required.");
    }
}