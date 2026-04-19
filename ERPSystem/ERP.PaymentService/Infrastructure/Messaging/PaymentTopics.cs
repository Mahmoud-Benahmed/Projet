namespace ERP.PaymentService.Infrastructure.Messaging
{
    public static class PaymentTopics
    {
        public const string InvoicePaid = "invoice.paid";
        public const string ClientUpdated = "client.updated";
        public const string InvoiceCreated = "invoice.created";
        public const string InvoiceUpdated = "invoice.updated";
    }
}
