namespace ERP.StockService.Infrastructure.Messaging.Events.FournisseurEvents;

public static class FournisseurTopics
{
    public const string Created = "fournisseur.created";
    public const string Updated = "fournisseur.updated";
    public const string Deleted = "fournisseur.deleted";
    public const string Restored = "fournisseur.restored";
}