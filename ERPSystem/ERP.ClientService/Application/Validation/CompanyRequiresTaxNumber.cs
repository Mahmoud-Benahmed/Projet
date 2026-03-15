using ERP.ClientService.Domain;
using System.ComponentModel.DataAnnotations;

namespace ERP.ClientService.Application.Validation
{
    public class CompanyRequiresTaxNumberAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext context)
        {
            var typeProperty = context.ObjectType.GetProperty("Type");
            var taxNumberProperty = context.ObjectType.GetProperty("TaxNumber");

            if (typeProperty == null || taxNumberProperty == null)
                return ValidationResult.Success;

            var type = typeProperty.GetValue(context.ObjectInstance) as ClientType?;
            var taxNumber = taxNumberProperty.GetValue(context.ObjectInstance) as string;

            if (type == ClientType.Company && string.IsNullOrWhiteSpace(taxNumber))
                return new ValidationResult("TaxNumber is required for Company clients.");

            return ValidationResult.Success;
        }
    }
}