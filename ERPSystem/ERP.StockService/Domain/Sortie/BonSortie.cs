
using ERP.StockService.Domain;
using ERP.StockService.Domain.Entre;

public sealed class BonSortie : PieceStock
{
    public Guid ClientId { get; private set; }
    private readonly List<LigneSortie> _lignes = [];
    public IReadOnlyCollection<LigneSortie> Lignes => _lignes.AsReadOnly();
    private BonSortie() { }

    public static BonSortie Create(string numero, Guid clientId, string? observation = null) =>
        new() { Id = Guid.NewGuid(), Numero = numero.Trim(), ClientId = clientId, Observation = observation?.Trim(), CreatedAt = DateTime.UtcNow };

    public void Update(Guid clientId, string? observation= null)
    {
        ClientId = clientId;
        base.Update(observation);
    }
    public LigneSortie AddLigne(Guid articleId, decimal qty, decimal price)
    {
        GuardNotDeleted();
        if (qty <= 0)
            throw new ArgumentException("Quantity must be > 0");

        if (price < 0)
            throw new ArgumentException("Price cannot be negative");

        var l = LigneSortie.Create(Id, articleId, qty, price);
        _lignes.Add(l);
        return l;
    }

    public void ClearLignes()
    {
        GuardNotDeleted();
        _lignes.Clear();
    }

    public override void ValidateLignes()
    {
        if (!_lignes.Any()) throw new InvalidOperationException("BonSortie must have at least one ligne.");
        foreach (var l in _lignes) l.Validate();
    }

    public decimal CalculateTotal() => _lignes.Sum(l => l.CalculateTotalLigne());
    private void GuardNotDeleted() { if (IsDeleted) throw new InvalidOperationException("Cannot modify a deleted BonSortie."); }
}