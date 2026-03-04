using ERP.ArticleService.Domain;
namespace ERP.ArticleService.Application.Interfaces
{

    public interface IArticleRepository
    {
        Task AddAsync(Article article);
        Task<Article?> GetByIdAsync(Guid id);
        Task<Article?> GetByCodeAsync(string code);
        Task<List<Article>> GetAllAsync();

        void Remove(Article article);
        Task SaveChangesAsync();

        // Paging & filtering
        Task<(List<Article> Items, int TotalCount)> GetPagedByCategoryIdAsync(Guid categoryId, int pageNumber, int pageSize);
        Task<(List<Article> Items, int TotalCount)> GetPagedByStatusAsync(bool isActive, int pageNumber, int pageSize);
        Task<(List<Article> Items, int TotalCount)> GetPagedByLibelleAsync(string libelleFilter, int pageNumber, int pageSize);
    }
}
