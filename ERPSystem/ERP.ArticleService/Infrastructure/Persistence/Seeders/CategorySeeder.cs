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
            var categoryNames = new[]
            {
                "Électronique",
                "Informatique",
                "Fournitures de bureau",
                "Mobilier",
                "Consommables",
                "Logiciels",
                "Réseaux & Télécommunications",
                "Outillage",
            };

            var random = new Random();
            foreach (var name in categoryNames)
            {
                try
                {
                    var tva = random.Next(1, 100);
                    var dto = new CategoryRequestDto(name, tva);
                    await _categoryService.CreateAsync(dto);
                    _logger.LogInformation("Seeded category: '{Name}'", name);
                }
                catch (InvalidOperationException)
                {
                    // CreateAsync throws if name already exists — safe to skip
                    _logger.LogInformation("Category '{Name}' already exists, skipping.", name);
                }
            }
        }
    }
}