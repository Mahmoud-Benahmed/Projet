namespace ERP.PaymentService.Domain.LocalCache
{
    public class Invoice
    {
        public Guid InvoiceId { get; set; }
        public Guid ClientId { get; set; }
        public decimal TotalTTC { get; set; }
        public decimal TotalPaid { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime InvoiceDate { get; set; }
        public string Status { get; set; } = string.Empty; // DRAFT, UNPAID, PAID, CANCELLED
        public bool LateFeeApplied { get; set; }
        public decimal LateFeeAmount { get; set; }
    }
}
