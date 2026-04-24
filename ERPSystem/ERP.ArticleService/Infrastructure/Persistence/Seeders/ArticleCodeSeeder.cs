using ERP.ArticleService.Domain;
using Microsoft.EntityFrameworkCore;
namespace ERP.ArticleService.Infrastructure.Persistence.Seeders
{

    public class ArticleCodeSeeder
    {
        private readonly ArticleDbContext _context;
        private readonly ILogger<ArticleCodeSeeder> _logger;

        public ArticleCodeSeeder(ArticleDbContext context, ILogger<ArticleCodeSeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            bool exists = await _context.ArticleCodes.AnyAsync();
            if (exists)
            {
                _logger.LogInformation("ArticleCode row already exists, skipping.");
                return;
            }

            // Single config row — prefix and padding must match FormatCode expectations
            ArticleCode articleCode = new ArticleCode("ART", 6);
            await _context.ArticleCodes.AddAsync(articleCode);
            await _context.SaveChangesAsync();
            _logger.LogInformation("ArticleCode config row seeded: ART, padding 6.");
        }
    }
}