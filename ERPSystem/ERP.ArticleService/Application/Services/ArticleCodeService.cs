using ERP.ArticleService.Application.Interfaces;

namespace ERP.ArticleService.Application.Services
{
    public class ArticleCodeService : IArticleCodeService
    {
        private readonly IArticleCodeRepository _articleCodeRepository;

        public ArticleCodeService(IArticleCodeRepository articleCodeRepository)
        {
            _articleCodeRepository = articleCodeRepository;
        }

        /// <summary>
        /// Delegates code generation to the repository which handles
        /// atomicity and row-level locking internally.
        /// </summary>
        public async Task<string> GenerateArticleCodeAsync()
        {
            return await _articleCodeRepository.GenerateArticleCodeAsync();
        }
    }
}