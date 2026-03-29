
using ERP.StockService.Domain;

public sealed class BonSortie : PieceStock
{
    public Guid ClientId { get; private set; }
    private readonly List<LigneSortie> _lignes = [];
    public IReadOnlyCollection<LigneSortie> Lignes => _lignes.AsReadOnly();
    private BonSortie() { }

    public static BonSortie Create(string numero, Guid clientId, string? observation = null) =>
        new() { Id = Guid.NewGuid(), Numero = numero.Trim(), ClientId = clientId, Observation = observation?.Trim(), CreatedAt = DateTime.UtcNow };

    public LigneSortie AddLigne(Guid articleId, decimal qty, decimal price)
    {
        GuardNotDeleted();
        if (qty <= 0)
            throw new ArgumentException("Quantity must be > 0");

        if (price < 0)
            throw new ArgumentException("Price cannot be negative");

        var l = LigneSortie.Create(Id, articleId, qty, price);
        _lignes.Add(l);
        UpdatedAt = DateTime.UtcNow;
        return l;
    }

    public void RemoveLigne(Guid ligneId)
    {
        GuardNotDeleted();

        var ligne = _lignes.FirstOrDefault(l => l.Id == ligneId);
        if (ligne is null)
            throw new KeyNotFoundException($"Ligne with Id '{ligneId}' not found");

        _lignes.Remove(ligne);

        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateLigne(Guid ligneId, decimal qty, decimal price)
    {
        GuardNotDeleted();
        var ligne = _lignes.FirstOrDefault(l => l.Id == ligneId) ?? throw new InvalidOperationException("Ligne not found.");
        ligne.Update(qty, price);
        UpdatedAt = DateTime.UtcNow;
    }


    public override void ValidateLignes()
    {
        if (!_lignes.Any()) throw new InvalidOperationException("BonSortie must have at least one ligne.");
        foreach (var l in _lignes) l.Validate();
    }

    public decimal CalculateTotal() => _lignes.Sum(l => l.CalculateTotalLigne());
    private void GuardNotDeleted() { if (IsDeleted) throw new InvalidOperationException("Cannot modify a deleted BonSortie."); }
}