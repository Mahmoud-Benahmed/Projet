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

            var seedData = SeedDataConstants.Articles.All;

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