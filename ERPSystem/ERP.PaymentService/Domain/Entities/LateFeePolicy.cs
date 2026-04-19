using ERP.PaymentService.Domain.Enums;

namespace ERP.PaymentService.Domain.Entities
{
    public class LateFeePolicy
    {
        public Guid Id { get; private set; }
        public decimal FeePercentage { get; private set; }
        public FeeType FeeType { get; private set; }
        public int GracePeriodDays { get; private set; }
        public bool IsActive { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        private LateFeePolicy() { }

        public LateFeePolicy(decimal feePercentage, FeeType feeType, int gracePeriodDays)
        {
            Id = Guid.NewGuid();
            FeePercentage = feePercentage;
            FeeType = feeType;
            GracePeriodDays = gracePeriodDays;
            IsActive = false;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public decimal CalculateTotalFee(decimal totalTTC, int daysOverdue = 0)
        {
            return FeeType switch
            {
                FeeType.PERCENTAGE => totalTTC * (FeePercentage / 100),
                FeeType.FIXED_PER_DAY => FeePercentage * daysOverdue,
                _ => 0
            };
        }

        public bool IsOverdue(DateTime dueDate)
        {
            return DateTime.UtcNow > dueDate.AddDays(GracePeriodDays);
        }

        public void Update(decimal feePercentage, FeeType feeType, int gracePeriodDays)
        {
            FeePercentage = feePercentage;
            FeeType = feeType;
            GracePeriodDays = gracePeriodDays;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Activate()
        {
            IsActive = true;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Deactivate()
        {
            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
