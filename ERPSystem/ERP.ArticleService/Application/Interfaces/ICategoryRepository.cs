using ERP.ArticleService.Domain;

namespace ERP.ArticleService.Application.Interfaces
{
    public interface ICategoryRepository
    {
        Task AddAsync(Category category);
        Task<Category?> GetByIdAsync(Guid id);
        Task<Category?> GetByNameAsync(string name);
        Task<List<Category>> GetAllAsync();


        Task<Category?> GetByTVAsync(decimal tva);
        Task<List<Category>> GetBetweenTVAAsync(decimal min, decimal max);
        Task<List<Category>> GetHigherThanTVAAsync(decimal tva);
        Task<List<Category>> GetBelowTVAAsync(decimal tva);


        void Remove(Category category);
        Task SaveChangesAsync();

        // =========================
        // PAGING / FILTERING
        // =========================
        Task<(List<Category> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize);

        Task<(List<Category> Items, int TotalCount)> GetPagedByNameAsync(
            string nameFilter,
            int pageNumber,
            int pageSize);

        Task<(List<Category> Items, int TotalCount)> GetPagedByDateRangeAsync(
            DateTime from,
            DateTime to,
            int pageNumber,
            int pageSize);
    }
}