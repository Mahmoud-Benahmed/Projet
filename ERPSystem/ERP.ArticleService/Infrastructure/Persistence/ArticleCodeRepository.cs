using ERP.ArticleService.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ERP.ArticleService.Infrastructure.Persistence
{
    public class ArticleCodeRepository : IArticleCodeRepository
    {
        private readonly ArticleDbContext _context;

        public ArticleCodeRepository(ArticleDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Generates a new unique article code atomically.
        /// Uses a database transaction with row-level locking to prevent
        /// duplicate codes under concurrent requests.
        /// Example output: "ART-2026-000001", "ART-2026-000042"
        /// </summary>
        public async Task<string> GenerateArticleCodeAsync()
        {
            await using var transaction = await _context.Database
                .BeginTransactionAsync();
            try
            {
                var articleCode = await _context.ArticleCodes
                    .FromSqlRaw("SELECT TOP 1 * FROM ArticleCodes WITH (UPDLOCK, ROWLOCK)")
                    .FirstOrDefaultAsync();

                if (articleCode is null)
                    throw new InvalidOperationException(
                        "No ArticleCode configuration row found. " +
                        "Please seed the ArticleCodes table with an initial row.");

                articleCode.Increment();

                var generatedCode = articleCode.FormatCode(DateTime.UtcNow.Year);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return generatedCode;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}