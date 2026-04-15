namespace ERP.InvoiceService.Infrastructure.Messaging.Events;

public static class InvoiceTopics
{
    public const string Created = "invoice.created";
    public const string Cancelled = "invoice.cancelled";
}