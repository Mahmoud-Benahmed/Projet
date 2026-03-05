using ERP.ArticleService.Application.Interfaces;
using ERP.ArticleService.Domain;
using Microsoft.EntityFrameworkCore;
using System.Drawing;

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

                foreach (var name in categoryNames)
                {
                    try
                    {
                        await _categoryService.CreateAsync(name);
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