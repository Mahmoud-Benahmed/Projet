namespace ERP.InvoiceService.Domain.LocalCache.Client;

public sealed class ClientCategoryCache
{
    public Guid ClientId { get; private set; }
    public Guid CategoryId { get; private set; }
    public DateTime AssignedAt { get; private set; }

    // Navigation properties
    public ClientCache? Client { get; private set; } = default!;
    public CategoryCache? Category { get; private set; } = default!;

    // EF Core constructor
    private ClientCategoryCache() { }
    public static ClientCategoryCache Create(
        Guid clientId,
        Guid categoryId) =>
        new()
        {
            ClientId = clientId,
            CategoryId = categoryId,
            AssignedAt = DateTime.UtcNow,
        };
}