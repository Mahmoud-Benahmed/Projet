namespace ERP.PaymentService.Infrastructure.Messaging.Events.InvoiceEvents
{
    public class InvoicePaidEvent
    {
        public Guid InvoiceId { get; set; }
        public Guid ClientId { get; set; }
        public decimal TotalTTC { get; set; }
        public decimal TotalPaid { get; set; }
        public DateTime PaidAt { get; set; }
    }
}
