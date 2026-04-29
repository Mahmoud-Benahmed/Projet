namespace ERP.PaymentService.Infrastructure.Messaging;

public static class PaymentTopics
{
    public const string Created = "payment.created";
    public const string Cancelled = "payment.cancelled";
    public const string InvoicePaid = "payment.invoice-paid";
}