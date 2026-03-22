namespace ERP.ClientService.Domain;

/// <summary>
/// Join entity between Client and Category.
/// Carries assignment metadata — who assigned and when.
/// Owned by Client — never created directly from Category.
/// </summary>
public sealed class ClientCategory
{
    public Guid ClientId { get; private set; }
    public Guid CategoryId { get; private set; }
    public Guid AssignedById { get; private set; }  // userId who made the assignment
    public DateTime AssignedAt { get; private set; }

    // Navigation properties
    public Client? Client { get; private set; } = default!;
    public Category? Category { get; private set; } = default!;

    // EF Core constructor
    private ClientCategory() { }

    internal static ClientCategory Create(
        Guid clientId,
        Guid categoryId,
        Guid assignedById) =>
        new()
        {
            ClientId = clientId,
            CategoryId = categoryId,
            AssignedById = assignedById,
            AssignedAt = DateTime.UtcNow,
        };
}