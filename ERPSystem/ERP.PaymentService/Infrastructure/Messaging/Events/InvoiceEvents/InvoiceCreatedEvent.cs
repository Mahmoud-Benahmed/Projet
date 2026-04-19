namespace ERP.PaymentService.Infrastructure.Messaging.Events.InvoiceEvents
{
    public class InvoiceCreatedEvent
    {
        public Guid InvoiceId { get; set; }
        public Guid ClientId { get; set; }
        public decimal TotalTTC { get; set; }
        public decimal TotalPaid { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime InvoiceDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool LateFeeApplied { get; set; }
        public decimal LateFeeAmount { get; set; }
    }

    public class InvoiceUpdatedEvent
    {
        public Guid InvoiceId { get; set; }
        public Guid ClientId { get; set; }
        public decimal TotalTTC { get; set; }
        public decimal TotalPaid { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime InvoiceDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool LateFeeApplied { get; set; }
        public decimal LateFeeAmount { get; set; }
    }
}
