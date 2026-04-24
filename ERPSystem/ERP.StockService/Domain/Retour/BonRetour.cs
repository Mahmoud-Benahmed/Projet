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

    // ---------------- UPDATE BON ----------------
    public void Update(string motif, string? observation)
    {

        if (string.IsNullOrWhiteSpace(motif))
            throw new ArgumentException("Motif is required.");

        Motif = motif.Trim();

        base.Update(observation);
    }

    public void ClearLignes()
    {
        _lignes.Clear();
    }

    public LigneRetour AddLigne(Guid articleId, decimal qty, decimal price)
    {

        if (articleId == Guid.Empty)
            throw new ArgumentException("ArticleId is required.");

        if (qty <= 0)
            throw new ArgumentException("Quantity must be > 0.");

        if (price < 0)
            throw new ArgumentException("Price cannot be negative.");

        if (_lignes.Any(l => l.ArticleId == articleId))
            throw new InvalidOperationException("Article already exists in lignes.");

        LigneRetour ligne = LigneRetour.Create(Id, articleId, qty, price);
        _lignes.Add(ligne);

        return ligne;
    }


    // ---------------- VALIDATE ----------------
    public override void ValidateLignes()
    {
        if (!_lignes.Any())
            throw new InvalidOperationException("BonRetour must have at least one ligne.");
        foreach (LigneRetour l in _lignes)
            l.Validate();
    }

    // ---------------- CALCULATE TOTAL ----------------
    public decimal CalculateTotal() => _lignes.Sum(l => l.CalculateTotalLigne());

}

public enum RetourSourceType { BonSortie, BonEntre }