using ERP.PaymentService.Domain.Enums;

namespace ERP.PaymentService.Domain.Entities
{
    public class Payment
    {
        public Guid Id { get; private set; }
        public Guid InvoiceId { get; private set; }
        public Guid ClientId { get; private set; }
        public decimal Amount { get; private set; }
        public DateTime PaymentDate { get; private set; }
        public PaymentMethod Method { get; private set; }
        public PaymentStatus Status { get; private set; }
        public decimal LateFeeApplied { get; private set; }
        public bool IsDeleted { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        private Payment() { }

        public Payment(
            Guid invoiceId,
            Guid clientId,
            decimal amount,
            PaymentMethod method,
            DateTime paymentDate)
        {
            Id = Guid.NewGuid();
            InvoiceId = invoiceId;
            ClientId = clientId;
            Amount = amount;
            Method = method;
            PaymentDate = paymentDate;
            Status = PaymentStatus.COMPLETED;
            LateFeeApplied = 0;
            IsDeleted = false;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void ApplyLateFee(decimal fee)
        {
            LateFeeApplied = fee;
            UpdatedAt = DateTime.UtcNow;
        }

        public void CheckAndUpdateStatus()
        {
            // Recalculates invoice status after each payment (triggered externally via service)
            UpdatedAt = DateTime.UtcNow;
        }

        public void Update(decimal amount, PaymentMethod method, DateTime paymentDate)
        {
            Amount = amount;
            Method = method;
            PaymentDate = paymentDate;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Delete()
        {
            IsDeleted = true;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Restore()
        {
            IsDeleted = false;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
