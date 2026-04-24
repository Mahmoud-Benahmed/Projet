using ERP.ArticleService.Application.DTOs;
using ERP.ArticleService.Application.Interfaces;

namespace ERP.ArticleService.Infrastructure.Persistence.Seeders
{
    public class CategorySeeder
    {
        private readonly ICategoryService _categoryService;
        private readonly ILogger<CategorySeeder> _logger;

        public CategorySeeder(ICategoryService categoryService, ILogger<CategorySeeder> logger)
        {
            _categoryService = categoryService;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            foreach ((string? name, int tva) in SeedDataConstants.Categories.All)
            {
                try
                {
                    CategoryRequestDto dto = new CategoryRequestDto(name, tva);
                    await _categoryService.CreateAsync(dto);
                    _logger.LogInformation("✓ Seeded category: '{Name}' (TVA: {TVA}%)", name, tva);
                }
                catch (InvalidOperationException)
                {
                    _logger.LogInformation("→ Category '{Name}' already exists, skipping.", name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "✗ Failed to seed category '{Name}'", name);
                }
            }
        }
    }
}