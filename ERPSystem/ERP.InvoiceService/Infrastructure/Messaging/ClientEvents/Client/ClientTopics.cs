namespace ERP.InvoiceService.Infrastructure.Messaging.ClientEvents.Client;

public static class ClientTopics
{
    public const string Created = "client.created";
    public const string Updated = "client.updated";
    public const string Deleted = "client.deleted";
    public const string Restored = "client.restored";
}