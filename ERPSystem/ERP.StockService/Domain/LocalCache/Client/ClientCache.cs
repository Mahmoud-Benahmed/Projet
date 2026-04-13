namespace ERP.StockService.Domain.LocalCache.Client;

public class ClientCache
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string Address { get; private set; } = default!;
    public string? Phone { get; private set; }
    public int? DuePaymentPeriod { get; private set; }
    public string? TaxNumber { get; private set; }
    public decimal? CreditLimit { get; private set; }  // ← Made nullable
    public int? DelaiRetour { get; private set; }
    public bool IsBlocked { get; private set; } = false;
    public bool IsDeleted { get; private set; } = false;

    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Simple List<> with private setter — EF maps this without any Navigation() config
    public List<ClientCategoryCache> ClientCategories { get; private set; } = [];

    private ClientCache() { }

    public static ClientCache Create(
        Guid id,
        string name, 
        string email, 
        string address,
        DateTime createdAt, 
        bool isBlocked = false, 
        bool isDeleted = false,
        DateTime? updatedAt = null,
        decimal? creditLimit = null,  // ← Made nullable with default
        string? phone = null, 
        string? taxNumber = null,
        int? delaiRetour = null,
        int? duePaymentPeriod = null)
    {
        ValidateName(name);
        ValidateEmail(email);
        ValidateAddress(address);
        ValidateCreditLimit(creditLimit);
        ValidateDelaiRetour(delaiRetour);
        ValidateDuePaymentPeriod(duePaymentPeriod);

        return new ClientCache
        {
            Id = id,
            Name = name.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            Address = address.Trim(),
            Phone = phone?.Trim(),
            TaxNumber = taxNumber?.Trim(),
            CreditLimit = creditLimit,
            DelaiRetour = delaiRetour,
            DuePaymentPeriod = duePaymentPeriod,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
        };
    }

    public void Update(
        string name,
        string email, 
        string address,
        DateTime createdAt, 
        bool isBlocked = false, 
        bool isDeleted = false,
        DateTime? updatedAt= null,
        decimal? creditLimit = null,  // ← Made nullable with default
        string? phone = null, 
        string? taxNumber = null,
        int? delaiRetour = null,
        int? duePaymentPeriod = null)
    {
        ValidateName(name);
        ValidateEmail(email);
        ValidateAddress(address);

        Name = name.Trim();
        Email = email.Trim().ToLowerInvariant();
        Address = address.Trim();
        Phone = phone?.Trim();
        TaxNumber = taxNumber?.Trim();
        CreditLimit = creditLimit;
        DelaiRetour = delaiRetour;
        DuePaymentPeriod = duePaymentPeriod;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        IsBlocked = isBlocked;
        IsDeleted = isDeleted;
    }

    public ClientCategoryCache AddCategory(CategoryCache category)
    {
        GuardNotDeleted();
        if (!category.IsActive)
            throw new InvalidOperationException(
                $"Category '{category.Name}' is not active.");

        if (ClientCategories.Any(cc => cc.CategoryId == category.Id))
            throw new InvalidOperationException(
                $"Client already has category '{category.Name}'.");

        var clientCategory = ClientCategoryCache.Create(Id, category.Id);
        ClientCategories.Add(clientCategory);

        UpdatedAt = DateTime.UtcNow;
        return clientCategory;
    }

    public void RemoveCategory(CategoryCache category)
    {
        GuardNotDeleted();

        var existing = ClientCategories
            .FirstOrDefault(cc => cc.CategoryId == category.Id);

        if (existing is null)
            throw new InvalidOperationException(
                $"Client does not have category '{category.Name}'.");

        ClientCategories.Remove(existing);

        // Recalculate effective values after removing category
        CreditLimit = CreditLimit;
        DelaiRetour = DelaiRetour;
        DuePaymentPeriod = DuePaymentPeriod;
        UpdatedAt = DateTime.UtcNow;
    }


    public void Block()
    {
        GuardNotDeleted();
        if (IsBlocked) return;
        IsBlocked = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Unblock()
    {
        if (!IsBlocked) return;
        IsBlocked = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Delete()
    {
        if (IsDeleted) return;
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Restore()
    {
        if (!IsDeleted) return;
        IsDeleted = false;
        UpdatedAt = DateTime.UtcNow;
    }


    public bool CanPlaceOrder(decimal orderAmount, decimal currentBalance)
    {
        if (IsBlocked || IsDeleted) return false;

        // If no credit limit set, allow order
        if (!CreditLimit.HasValue) return true;

        return currentBalance + orderAmount <= CreditLimit.Value;
    }

    public bool IsWithinDelaiRetour(DateTime documentDate)
    {
        var window = DelaiRetour;
        if (!window.HasValue) return false;
        return (DateTime.UtcNow - documentDate).TotalDays <= window.Value;
    }

    private void GuardNotDeleted()
    {
        if (IsDeleted)
            throw new InvalidOperationException("Cannot modify a deleted client.");
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        if (name.Trim().Length > 200)
            throw new ArgumentException("Name cannot exceed 200 characters.", nameof(name));
    }

    private static void ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));
        if (!email.Contains('@'))
            throw new ArgumentException("Email is not valid.", nameof(email));
    }

    private static void ValidateAddress(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("Address is required.", nameof(address));
    }

    private static void ValidateCreditLimit(decimal? creditLimit)
    {
        if (creditLimit.HasValue && creditLimit <= 0)
            throw new ArgumentException("Credit limit must be positive.", nameof(creditLimit));
    }

    private static void ValidateDelaiRetour(int? days)
    {
        if (days.HasValue && days <= 0)
            throw new ArgumentException("Return delay must be at least 1 day.", nameof(days));
    }

    private static void ValidateDuePaymentPeriod(int? days)
    {
        if (days.HasValue && days <= 0)
            throw new ArgumentException(
                "Due payment period must be at least 1 day.", nameof(days));
    }
}