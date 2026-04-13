namespace ERP.StockService.Infrastructure.Messaging.FournisseurEvents;

public static class FrournisseurTopics
{
    public const string Created = "fournisseur.created";
    public const string Updated = "fournisseur.updated";
    public const string Deleted = "fournisseur.deleted";
    public const string Restored = "fournisseur.restored";
}