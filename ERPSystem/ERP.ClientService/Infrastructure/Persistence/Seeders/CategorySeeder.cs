using Microsoft.EntityFrameworkCore;
using ERP.ClientService.Domain;

namespace ERP.ClientService.Infrastructure.Persistence.Seeders;
public class CategorySeeder
{
    private readonly ClientDbContext _context;
    private readonly ILogger<CategorySeeder> _logger;

    public CategorySeeder(ClientDbContext context, ILogger<CategorySeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        if (await _context.Categories.AnyAsync())
        {
            _logger.LogInformation("Categories already seeded — skipping.");
            return;
        }

        var categories = BuildCategories();

        await _context.Categories.AddRangeAsync(categories);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Seeded {Count} categories.", categories.Count);
    }

    // ── Seed data ─────────────────────────────────────────────────────────────

    private static List<Category> BuildCategories() =>
    [
        // Standard retail client — no special pricing
        Category.Create(
            name:                  "Standard",
            code:                  "STD",
            delaiRetour:           15,
            useBulkPricing:        false,
            discountRate:          null,
            creditLimitMultiplier: null),

        // VIP — generous return window, 10% discount, 150% credit multiplier
        Category.Create(
            name:                  "VIP",
            code:                  "VIP",
            delaiRetour:           60,
            useBulkPricing:        true,
            discountRate:          0.10m,
            creditLimitMultiplier: 1.5m),

        // Wholesale — bulk pricing, 15% discount, doubled credit limit
        Category.Create(
            name:                  "Wholesale",
            code:                  "WHL",
            delaiRetour:           30,
            useBulkPricing:        true,
            discountRate:          0.15m,
            creditLimitMultiplier: 2.0m),

        // Public sector — no discount but extended return window
        Category.Create(
            name:                  "Public Sector",
            code:                  "PUB",
            delaiRetour:           45,
            useBulkPricing:        false,
            discountRate:          null,
            creditLimitMultiplier: 1.2m),

        // Reseller — bulk pricing, 20% discount
        Category.Create(
            name:                  "Reseller",
            code:                  "RSL",
            delaiRetour:           30,
            useBulkPricing:        true,
            discountRate:          0.20m,
            creditLimitMultiplier: 1.8m),

        // New client — restricted: short return window, no credit multiplier
        Category.Create(
            name:                  "New Client",
            code:                  "NEW",
            delaiRetour:           7,
            useBulkPricing:        false,
            discountRate:          null,
            creditLimitMultiplier: null),

        // Inactive category — to test deactivation logic
        BuildInactiveCategory(),
    ];

    private static Category BuildInactiveCategory()
    {
        var cat = Category.Create(
            name: "Legacy",
            code: "LGC",
            delaiRetour: 10,
            useBulkPricing: false,
            discountRate: null,
            creditLimitMultiplier: null);

        cat.Deactivate();
        return cat;
    }
}