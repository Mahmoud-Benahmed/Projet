namespace ERP.ArticleService.Application.Interfaces
{
    public interface IArticleCodeService
    {
        // =========================
        // GENERATE
        // =========================

        /// <summary>
        /// Generates a new unique article code.
        /// Delegates atomicity and locking to the repository layer.
        /// Example output: "ART-2026-000001"
        /// </summary>
        Task<string> GenerateArticleCodeAsync();
    }
}