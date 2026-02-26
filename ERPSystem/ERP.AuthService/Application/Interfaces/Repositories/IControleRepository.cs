using ERP.AuthService.Domain;

namespace ERP.AuthService.Application.Interfaces.Repositories
{
    public interface IControleRepository
    {
        Task<Controle?> GetByIdAsync(Guid id);
        Task<Controle?> GetByLibelleAsync(string libelle);
        Task<List<Controle>> GetAllAsync();
        Task<List<Controle>> GetByCategoryAsync(string category);
        Task AddAsync(Controle controle);
        Task UpdateAsync(Controle controle);
        Task DeleteAsync(Guid id);
        Task DeleteAllAsync();
        Task<long> CountAsync();

    }
}
