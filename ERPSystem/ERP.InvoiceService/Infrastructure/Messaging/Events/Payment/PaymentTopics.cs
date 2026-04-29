namespace ERP.InvoiceService.Infrastructure.Messaging.Events.Payment;

public static class PaymentTopics
{
    public const string Cancelled = "payment.cancelled";
    public const string InvoicePaid = "payment.invoice-paid";
}