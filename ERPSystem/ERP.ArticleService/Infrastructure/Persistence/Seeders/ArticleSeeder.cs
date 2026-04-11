using DotNetEnv;
using ERP.ArticleService.Application.DTOs;
using ERP.ArticleService.Application.Interfaces;

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
                // Électronique (TVA 19%)
                ("Écran 27 pouces Full HD",               1299.99m, "Électronique", UnitEnum.Piece, 19),
                ("Clavier mécanique sans fil",              349.99m, "Électronique", UnitEnum.Piece, 19),
                ("Souris ergonomique Bluetooth",            199.99m, "Électronique", UnitEnum.Piece, 19),

                // Informatique (TVA 19%)
                ("Laptop Core i7 16Go RAM",               5499.99m, "Informatique", UnitEnum.Piece, 19),
                ("SSD 1To NVMe",                           599.99m, "Informatique", UnitEnum.Piece, 19),
                ("Station d'accueil USB-C",                799.99m, "Informatique", UnitEnum.Piece, 19),

                // Fournitures de bureau (TVA 19%)
                ("Ramette papier A4 500 feuilles",          49.99m, "Fournitures de bureau", UnitEnum.Piece, 19),
                ("Stylos bille lot de 10",                  29.99m, "Fournitures de bureau", UnitEnum.Piece, 19),
                ("Classeur à levier A4",                    19.99m, "Fournitures de bureau", UnitEnum.Piece, 19),

                // Mobilier (TVA 19%)
                ("Bureau réglable en hauteur",            2999.99m, "Mobilier", UnitEnum.Piece, 19),
                ("Chaise ergonomique de bureau",          1899.99m, "Mobilier", UnitEnum.Piece, 19),
                ("Étagère modulable 5 niveaux",            699.99m, "Mobilier", UnitEnum.Piece, 19),

                // Consommables (TVA 19%)
                ("Cartouche d'encre noire HP",              89.99m, "Consommables", UnitEnum.Piece, 19),
                ("Toner laser Brother",                    149.99m, "Consommables", UnitEnum.Piece, 19),
                ("Papier photo brillant A4 x50",            59.99m, "Consommables", UnitEnum.Piece, 19),

                // Logiciels (TVA 19%)
                ("Licence Microsoft Office 2024",         1199.99m, "Logiciels", UnitEnum.Piece, 19),
                ("Antivirus Pro 1 an",                     199.99m, "Logiciels", UnitEnum.Piece, 19),
                ("Suite Adobe Creative Cloud",            2999.99m, "Logiciels", UnitEnum.Piece, 19),

                // Réseaux & Télécommunications (TVA 19%)
                ("Switch 24 ports Gigabit",               1499.99m, "Réseaux & Télécommunications", UnitEnum.Piece, 19),
                ("Routeur Wi-Fi 6 AX3000",                 899.99m, "Réseaux & Télécommunications", UnitEnum.Piece, 19),
                ("Câble RJ45 Cat6 10m",                     49.99m, "Réseaux & Télécommunications", UnitEnum.Meter, 19),
                
                // Outillage (TVA 19%)
                ("Tournevis électrique sans fil",          299.99m, "Outillage", UnitEnum.Piece, 19),
                ("Multimètre numérique",                   149.99m, "Outillage", UnitEnum.Piece, 19),
                ("Kit d'outils informatiques",              99.99m, "Outillage", UnitEnum.Piece, 19),

                // Food items (TVA 7% - reduced rate)
                ("Café en grains 1kg",                      89.99m, "Alimentation", UnitEnum.Kilogram, 7),
                ("Thé vert 500g",                           49.99m, "Alimentation", UnitEnum.Gram, 7),
                ("Eau minérale 1.5L x 6",                   19.99m, "Alimentation", UnitEnum.Liter, 7),

                // Services (TVA 19%)
                ("Heure de consulting IT",                 150.00m, "Services", UnitEnum.Hour,  19),
                ("Journée de formation",                   800.00m, "Services", UnitEnum.Day,   19),
            };

            var usedBarcodes = new HashSet<string>();
            var random = new Random();

            foreach (var (libelle, prix, categoryName, unit, tva) in seedData)
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
                    // Generate unique barcode
                    string barCode;
                    do
                    {
                        barCode = GenerateEAN13();
                    } while (usedBarcodes.Contains(barCode));
                    usedBarcodes.Add(barCode);

                    // Ensure TVA is valid (must be > 0)
                    var validTva = tva > 0 ? tva : 19;

                    var createRequest = new CreateArticleRequestDto(
                        Libelle: libelle,
                        Prix: prix,
                        Unit: unit,
                        CategoryId: categoryId,
                        BarCode: barCode,
                        TVA: validTva
                    );

                    var article = await _articleService.CreateAsync(createRequest);

                    _logger.LogInformation(
                        "Seeded article: '{Code}' - {Libelle} (TVA: {TVA}%, Unit: {Unit})",
                        article.CodeRef, article.Libelle, article.TVA, article.Unit);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to seed article '{Libelle}' for category '{CategoryName}'.",
                        libelle, categoryName);
                }
            }
        }

        private static string GenerateEAN13()
        {
            var random = new Random();
            var digits = new int[12];

            // Ensure first digit is not zero (EAN-13 standard)
            digits[0] = random.Next(1, 10);

            for (int i = 1; i < 12; i++)
                digits[i] = random.Next(0, 10);

            // Calculate check digit using EAN-13 algorithm
            int sum = 0;
            for (int i = 0; i < 12; i++)
            {
                // Multiply by 1 for odd positions (1,3,5,7,9,11) and 3 for even positions (2,4,6,8,10,12)
                int multiplier = (i % 2 == 0) ? 1 : 3;
                sum += digits[i] * multiplier;
            }

            int checkDigit = (10 - (sum % 10)) % 10;

            return string.Concat(digits) + checkDigit;
        }
    }
}