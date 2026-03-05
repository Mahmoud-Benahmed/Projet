using ERP.ArticleService.Application.Interfaces;
using ERP.ArticleService.Domain;
using Microsoft.EntityFrameworkCore;

namespace ERP.ArticleService.Infrastructure.Persistence.Seeders
{
    public class ArticleSeeder
    {
        private readonly IArticleService _articleService;
        private readonly ICategoryService _categoryService;
        private readonly ILogger<ArticleSeeder> _logger;

        public ArticleSeeder(
            IArticleService articleService,
            ICategoryService categoryService,
            ILogger<ArticleSeeder> logger)
        {
            _articleService = articleService;
            _categoryService = categoryService;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            // Load all categories by name for lookup
            var categories = await _categoryService.GetAllAsync();
            var categoryMap = categories.ToDictionary(c => c.Name, c => c.Id);

            var seedData = new[]
            {
                // Électronique
                ("Écran 27 pouces Full HD",               1299.99m, "Électronique"),
                ("Clavier mécanique sans fil",              349.99m, "Électronique"),
                ("Souris ergonomique Bluetooth",            199.99m, "Électronique"),
                // Informatique
                ("Laptop Core i7 16Go RAM",               5499.99m, "Informatique"),
                ("SSD 1To NVMe",                           599.99m, "Informatique"),
                ("Station d'accueil USB-C",                799.99m, "Informatique"),
                // Fournitures de bureau
                ("Ramette papier A4 500 feuilles",          49.99m, "Fournitures de bureau"),
                ("Stylos bille lot de 10",                  29.99m, "Fournitures de bureau"),
                ("Classeur à levier A4",                    19.99m, "Fournitures de bureau"),
                // Mobilier
                ("Bureau réglable en hauteur",            2999.99m, "Mobilier"),
                ("Chaise ergonomique de bureau",          1899.99m, "Mobilier"),
                ("Étagère modulable 5 niveaux",            699.99m, "Mobilier"),
                // Consommables
                ("Cartouche d'encre noire HP",              89.99m, "Consommables"),
                ("Toner laser Brother",                    149.99m, "Consommables"),
                ("Papier photo brillant A4 x50",            59.99m, "Consommables"),
                // Logiciels
                ("Licence Microsoft Office 2024",         1199.99m, "Logiciels"),
                ("Antivirus Pro 1 an",                     199.99m, "Logiciels"),
                ("Suite Adobe Creative Cloud",            2999.99m, "Logiciels"),
                // Réseaux & Télécommunications
                ("Switch 24 ports Gigabit",               1499.99m, "Réseaux & Télécommunications"),
                ("Routeur Wi-Fi 6 AX3000",                 899.99m, "Réseaux & Télécommunications"),
                ("Câble RJ45 Cat6 10m",                     49.99m, "Réseaux & Télécommunications"),
                // Outillage
                ("Tournevis électrique sans fil",          299.99m, "Outillage"),
                ("Multimètre numérique",                   149.99m, "Outillage"),
                ("Kit d'outils informatiques",              99.99m, "Outillage"),
            };

            foreach (var (libelle, prix, categoryName) in seedData)
            {
                if (!categoryMap.TryGetValue(categoryName, out var categoryId))
                {
                    _logger.LogWarning(
                        "Category '{CategoryName}' not found for article '{Libelle}', skipping.",
                        categoryName, libelle);
                    continue;
                }

                try
                {
                    var article = await _articleService.CreateAsync(libelle, prix, categoryId);
                    _logger.LogInformation(
                        "Seeded article: '{Code}' - {Libelle}", article.Code, article.Libelle);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to seed article '{Libelle}'.", libelle);
                }
            }
        }
    }
}