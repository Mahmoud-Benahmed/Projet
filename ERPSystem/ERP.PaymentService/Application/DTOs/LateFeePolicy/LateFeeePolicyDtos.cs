using ERP.PaymentService.Domain.Enums;

namespace ERP.PaymentService.Application.DTOs.LateFeePolicy
{
    public record CreateLateFeePolicyDto(
        decimal FeePercentage,
        FeeType FeeType,
        int GracePeriodDays);

    public record UpdateLateFeePolicyDto(
        decimal FeePercentage,
        FeeType FeeType,
        int GracePeriodDays);

    public record LateFeeePolicyDto(
        Guid Id,
        decimal FeePercentage,
        FeeType FeeType,
        int GracePeriodDays,
        bool IsActive,
        DateTime CreatedAt);
}
