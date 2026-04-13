using ERP.InvoiceService.Application.DTOs;
namespace ERP.InvoiceService.Domain.LocalCache.Client;

public sealed class CategoryCache
{

    // ── Identity ──────────────────────────────────────────────────────────────
    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public string Code { get; private set; } = default!;

    // ── Business rules ────────────────────────────────────────────────────────
    public int DelaiRetour { get; private set; }
    public int DuePaymentPeriod { get; private set; }
    public decimal? DiscountRate { get; private set; }  // 0.00 – 1.00
    public decimal? CreditLimitMultiplier { get; private set; }  // e.g. 1.5 = 150%
    public bool UseBulkPricing { get; private set; }

    // ── Status ────────────────────────────────────────────────────────────────
    public bool IsActive { get; private set; } = true;
    public bool IsDeleted { get; private set; } = false;

    // ── Audit ─────────────────────────────────────────────────────────────────
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // ── EF Core constructor ───────────────────────────────────────────────────
    private CategoryCache() { }

    // ── Factory ───────────────────────────────────────────────────────────────
    public static CategoryCache Create(
        Guid id,
        string name,
        string code,
        int delaiRetour,
        int duePaymentPeriod,          // <-- was the broken `int )` — fixed
        DateTime createdAt,
        bool useBulkPricing = false,
        decimal? discountRate = null,
        decimal? creditLimitMultiplier = null,
        bool isActive = true,
        bool isDeleted = false,
        DateTime? updatedAt= null)
    {
        ValidateName(name);
        ValidateCode(code);
        ValidateDelaiRetour(delaiRetour);
        ValidateDiscountRate(discountRate);
        ValidateCreditLimitMultiplier(creditLimitMultiplier);
        ValidateDuePaymentPeriod(duePaymentPeriod);            // <-- add this

        return new CategoryCache
        {
            Id = id,
            Name = name.Trim(),
            Code = code.Trim().ToUpperInvariant(),
            DelaiRetour = delaiRetour,
            UseBulkPricing = useBulkPricing,
            DiscountRate = discountRate,
            CreditLimitMultiplier = creditLimitMultiplier,
            DuePaymentPeriod = duePaymentPeriod,               // <-- add this
            IsDeleted = isDeleted,
            IsActive = isActive,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }


    // ── Update ────────────────────────────────────────────────────────────────
    public void Update(
    string name,
    string code,
    int delaiRetour,
    bool isActive,
    bool isDeleted,
    DateTime createdAt,
    int duePaymentPeriod,          // ← int, not int?
    DateTime? updatedAt= null,
    bool useBulkPricing = false,
    decimal? discountRate = null,
    decimal? creditLimitMultiplier = null)
    {   ValidateName(name);
        ValidateCode(code);
        ValidateDelaiRetour(delaiRetour);
        ValidateDuePaymentPeriod(duePaymentPeriod);
        ValidateDiscountRate(discountRate);
        ValidateCreditLimitMultiplier(creditLimitMultiplier);

        Name = name.Trim();
        Code = code.Trim().ToUpperInvariant();
        DelaiRetour = delaiRetour;
        DuePaymentPeriod = duePaymentPeriod;
        UseBulkPricing = useBulkPricing;
        DiscountRate = discountRate;
        CreditLimitMultiplier = creditLimitMultiplier;
        CreatedAt= createdAt;
        UpdatedAt = DateTime.UtcNow;
        IsDeleted= isDeleted;
        IsActive= isActive;
    }

    public void Delete()
    {
        GuardNotDeleted();
        IsDeleted = true;
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Restore()
    {
        IsDeleted = false;
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
    private void GuardNotDeleted()
    {
        if (IsDeleted)
            throw new InvalidOperationException("Cannot modify a deleted category.");
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        if (name.Trim().Length > 200)
            throw new ArgumentException("Name cannot exceed 200 characters.", nameof(name));
    }

    private static void ValidateCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required.", nameof(code));
        if (code.Trim().Length > 50)
            throw new ArgumentException("Code cannot exceed 50 characters.", nameof(code));
    }

    private static void ValidateDelaiRetour(int delaiRetour)
    {
        if (delaiRetour <= 0)
            throw new ArgumentException(
                "Return delay must be at least 1 day.", nameof(delaiRetour));
    }

    private static void ValidateDiscountRate(decimal? discountRate)
    {
        if (!discountRate.HasValue) return;
        if (discountRate < 0 || discountRate > 1)
            throw new ArgumentException(
                "Discount rate must be between 0 and 1 (0% – 100%).",
                nameof(discountRate));
    }

    private static void ValidateCreditLimitMultiplier(decimal? multiplier)
    {
        if (!multiplier.HasValue) return;
        if (multiplier < 0)
            throw new ArgumentException(
                "Credit limit multiplier must be positive.",
                nameof(multiplier));
    }

    private static void ValidateDuePaymentPeriod(int days)
    {
        if (days <= 0)
            throw new ArgumentException(
                "Due payment period must be at least 1 day.", nameof(days));
    }

    
}