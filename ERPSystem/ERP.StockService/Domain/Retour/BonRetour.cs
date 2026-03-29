using ERP.StockService.Domain;

public sealed class BonRetour : PieceStock
{
    public Guid SourceId { get; private set; }
    public RetourSourceType SourceType { get; private set; }
    public string Motif { get; private set; } = default!;

    private readonly List<LigneRetour> _lignes = [];
    public IReadOnlyCollection<LigneRetour> Lignes => _lignes.AsReadOnly();

    private BonRetour() { }

    // ---------------- CREATE ----------------
    public static BonRetour Create(
        string numero, Guid sourceId, RetourSourceType sourceType,
        string motif, string? observation = null)
    {
        if (string.IsNullOrWhiteSpace(numero))
            throw new ArgumentException("Numero is required.");

        if (sourceId == Guid.Empty)
            throw new ArgumentException("SourceId is required.");

        if (string.IsNullOrWhiteSpace(motif))
            throw new ArgumentException("Motif is required.");

        return new BonRetour
        {
            Id = Guid.NewGuid(),
            Numero = numero.Trim(),
            SourceId = sourceId,
            SourceType = sourceType,
            Motif = motif.Trim(),
            Observation = observation?.Trim(),
            CreatedAt = DateTime.UtcNow,
        };
    }

    // ---------------- ADD LIGNE ----------------
    public LigneRetour AddLigne(Guid articleId, decimal qty, decimal price, string? remarque = null)
    {
        GuardNotDeleted();

        if (articleId == Guid.Empty)
            throw new ArgumentException("ArticleId is required.");
        if (qty <= 0)
            throw new ArgumentException("Quantity must be > 0.");
        if (price < 0)
            throw new ArgumentException("Price cannot be negative.");

        // Optional business rule: prevent duplicate articles
        if (_lignes.Any(l => l.ArticleId == articleId))
            throw new InvalidOperationException("Article already exists in lignes.");

        var ligne = LigneRetour.Create(Id, articleId, qty, price, remarque);
        _lignes.Add(ligne);
        UpdatedAt = DateTime.UtcNow;
        return ligne;
    }

    // ---------------- REMOVE LIGNE ----------------
    public void RemoveLigne(Guid ligneId)
    {
        GuardNotDeleted();

        if (ligneId == Guid.Empty)
            throw new ArgumentException("LigneId is required.");
        if (!_lignes.Any())
            throw new InvalidOperationException("No lignes to remove.");

        var ligne = _lignes.FirstOrDefault(l => l.Id == ligneId)
            ?? throw new InvalidOperationException("Ligne not found.");

        // Business rule: cannot remove last ligne
        if (_lignes.Count == 1)
            throw new InvalidOperationException("Cannot remove the last ligne.");

        _lignes.Remove(ligne);
        UpdatedAt = DateTime.UtcNow;
    }

    // ---------------- UPDATE LIGNE ----------------
    public void UpdateLigne(Guid ligneId, decimal qty, decimal price, string? remarque=null)
    {
        GuardNotDeleted();

        if (ligneId == Guid.Empty)
            throw new ArgumentException("LigneId is required.");
        if (qty <= 0)
            throw new ArgumentException("Quantity must be > 0.");
        if (price < 0)
            throw new ArgumentException("Price cannot be negative.");

        var ligne = _lignes.FirstOrDefault(l => l.Id == ligneId)
            ?? throw new InvalidOperationException("Ligne not found.");

        ligne.Update(qty, price, remarque);
        UpdatedAt = DateTime.UtcNow;
    }

    // ---------------- UPDATE BON ----------------
    public void Update(string numero, string motif, string? observation = null)
    {
        GuardNotDeleted();

        if (string.IsNullOrWhiteSpace(numero))
            throw new ArgumentException("Numero is required.");
        if (string.IsNullOrWhiteSpace(motif))
            throw new ArgumentException("Motif is required.");

        Numero = numero.Trim();
        Motif = motif.Trim();
        Observation = observation?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    // ---------------- VALIDATE ----------------
    public override void ValidateLignes()
    {
        if (!_lignes.Any())
            throw new InvalidOperationException("BonRetour must have at least one ligne.");
        foreach (var l in _lignes)
            l.Validate();
    }

    // ---------------- CALCULATE TOTAL ----------------
    public decimal CalculateTotal() => _lignes.Sum(l => l.CalculateTotalLigne());

    // ---------------- GUARD ----------------
    private void GuardNotDeleted()
    {
        if (IsDeleted)
            throw new InvalidOperationException("Cannot modify a deleted BonRetour.");
    }
}

public enum RetourSourceType { BonSortie, BonEntre }