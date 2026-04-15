namespace ERP.StockService.Domain;

public class InvoiceBonSortieMapping
{
    public Guid Id { get; private set; }
    public Guid InvoiceId { get; private set; }
    public Guid BonSortieId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private InvoiceBonSortieMapping() { } // EF Core

    public InvoiceBonSortieMapping(Guid invoiceId, Guid bonSortieId)
    {
        Id = Guid.NewGuid();
        InvoiceId = invoiceId;
        BonSortieId = bonSortieId;
        CreatedAt = DateTime.UtcNow;
    }
}