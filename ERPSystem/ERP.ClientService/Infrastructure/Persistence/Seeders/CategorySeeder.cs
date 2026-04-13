using ERP.ClientService.Application.DTOs;
using ERP.ClientService.Application.Interfaces;

namespace ERP.ClientService.Infrastructure.Persistence.Seeders;

public class CategorySeeder
{
    private readonly ICategoryService _categoryService;
    private readonly ILogger<CategorySeeder> _logger;

    public CategorySeeder(ICategoryService categoryService, ILogger<CategorySeeder> logger)
    {
        _categoryService = categoryService;
        _logger = logger;
    }

    public async Task<List<CategoryResponseDto>> SeedAsync()
    {
        // Check if categories already exist
        var existingCategories = await _categoryService.GetAllAsync();
        if (existingCategories.Any())
        {
            _logger.LogInformation("Categories already seeded — returning existing.");
            return existingCategories;
        }

        var categories = BuildCategoryRequests();
        var createdCategories = new List<CategoryResponseDto>();

        foreach (var request in categories)
        {
            try
            {
                var category = await _categoryService.CreateAsync(request);
                createdCategories.Add(category);
                _logger.LogInformation("Seeded category: {Name} (Code: {Code}, Id: {Id})",
                    category.Name, category.Code, category.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to seed category: {Name}", request.Name);
            }
        }

        // Create and deactivate Legacy category
        try
        {
            var legacyRequest = new CreateCategoryRequestDto(
                Name: "Legacy",
                Code: "LGC",
                DelaiRetour: 10,
                DuePaymentPeriod: 30,
                UseBulkPricing: false,
                DiscountRate: null,
                CreditLimitMultiplier: null);

            var legacy = await _categoryService.CreateAsync(legacyRequest);
            await _categoryService.DeactivateAsync(legacy.Id);
            createdCategories.Add(legacy);
            _logger.LogInformation("Seeded and deactivated category: Legacy (Id: {Id})", legacy.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed Legacy category");
        }

        _logger.LogInformation("Category seeding completed. Created {Count} categories.", createdCategories.Count);
        return createdCategories;
    }

    private List<CreateCategoryRequestDto> BuildCategoryRequests() =>
    [
        new("Standard", "STD", 15, 30, false, null, null),
        new("VIP", "VIP", 60, 60, true, 0.10m, 1.5m),
        new("Wholesale", "WHL", 30, 45, true, 0.15m, 2.0m),
        new("Public Sector", "PUB", 45, 60, false, null, 1.2m),
        new("Reseller", "RSL", 30, 45, true, 0.20m, 1.8m),
        new("New Client", "NEW", 7, 15, false, null, null),
    ];
}