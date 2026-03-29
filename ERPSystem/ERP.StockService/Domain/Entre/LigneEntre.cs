namespace ERP.StockService.Domain.Entre;

public sealed class LigneEntre : LigneStock
{
    public Guid BonEntreId { get; private set; }
    public BonEntre? BonEntre { get; private set; }

    private LigneEntre() { }

    internal static LigneEntre Create(Guid bonEntreId, Guid articleId, decimal qty, decimal price)
    {
        var ligne = new LigneEntre
        {
            Id = Guid.NewGuid(),
            BonEntreId = bonEntreId,
            ArticleId = articleId,
            Quantity = qty,
            Price = price,
        };
        ligne.Validate();
        return ligne;
    }

    public void Update(decimal qty, decimal price)
    {
        Quantity = qty;
        Price = price;
        Validate();
    }

    public void Validate()
    {
        if (ArticleId == Guid.Empty)
            throw new InvalidOperationException("ArticleId is required.");

        if (Quantity <= 0)
            throw new InvalidOperationException("Quantity must be greater than zero.");

        if (Price < 0)
            throw new InvalidOperationException("Price cannot be negative.");
    }

    public decimal CalculateTotalLigne() => Quantity * Price;
}