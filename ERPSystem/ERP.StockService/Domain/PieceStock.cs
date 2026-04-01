namespace ERP.StockService.Domain;

public abstract class PieceStock
{
    public Guid Id { get; protected set; }
    public string Numero { get; protected set; } = default!;
    public string? Observation { get; protected set; }
    public bool IsDeleted { get; protected set; }
    public DateTime CreatedAt { get; protected set; }
    public DateTime? UpdatedAt { get; private set; }

    public abstract void ValidateLignes();

    public void Delete()
    {
        if (IsDeleted) return;
        IsDeleted = true;
    }

    public void Update(string numero, string? observation = null)
    {
        if (string.IsNullOrWhiteSpace(numero))
            throw new ArgumentException("Numero is required.");
        Numero = numero.Trim();
        Observation = observation?.Trim();
    }


}