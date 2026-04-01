// IFournisseurRepository
using ERP.StockService.Application.DTOs;
using ERP.StockService.Domain;

public interface IFournisseurRepository
{
    Task AddAsync(Fournisseur f);
    Task SaveChangesAsync();
    Task<Fournisseur?> GetByIdAsync(Guid id);
    Task<Fournisseur?> GetByIdDeletedAsync(Guid id);
    Task<(List<Fournisseur> Items, int TotalCount)> GetPagedByNameAsync(
    string nameFilter, int page, int size);
    Task<(List<Fournisseur> Items, int TotalCount)> GetAllAsync(int page, int size);
    Task<(List<Fournisseur> Items, int TotalCount)> GetPagedDeletedAsync(int page, int size);
    Task<FournisseurStatsDto> GetStatsAsync();
}

// IBonEntreRepository
public interface IBonEntreRepository
{
    Task AddAsync(BonEntre b);
    Task SaveChangesAsync();
    Task<BonEntre?> GetByIdAsync(Guid id);
    Task<BonEntre?> GetByIdDeletedAsync(Guid id);
    Task<(List<BonEntre> Items, int TotalCount)> GetAllAsync(int page, int size);
    Task<(List<BonEntre> Items, int TotalCount)> GetByFournisseurAsync(Guid fournisseurId, int page, int size);
    Task<(List<BonEntre> Items, int TotalCount)> GetPagedDeletedAsync(int page, int size);
    Task<(List<BonEntre> Items, int TotalCount)> GetPagedByDateRangeAsync(DateTime from, DateTime to, int page, int size);
    Task<BonStatsDto> GetStatsAsync();
}

public interface IBonSortieRepository
{
    Task AddAsync(BonSortie b);
    Task SaveChangesAsync();
    Task<BonSortie?> GetByIdAsync(Guid id);
    Task<BonSortie?> GetByIdDeletedAsync(Guid id);
    Task<(List<BonSortie> Items, int TotalCount)> GetAllAsync(int page, int size);
    Task<(List<BonSortie> Items, int TotalCount)> GetByClientAsync(Guid clientId, int page, int size);
    Task<(List<BonSortie> Items, int TotalCount)> GetPagedDeletedAsync(int page, int size);
    Task<(List<BonSortie> Items, int TotalCount)> GetPagedByDateRangeAsync(DateTime from, DateTime to, int page, int size);
    Task<(List<BonSortie> Items, int TotalCount)> GetPagedByClientAsync(Guid clientId, int page, int size);
    Task<BonStatsDto> GetStatsAsync();
}
public interface IBonRetourRepository
{
    Task AddAsync(BonRetour b);
    Task SaveChangesAsync();
    Task<BonRetour?> GetByIdAsync(Guid id);
    Task<BonRetour?> GetByIdDeletedAsync(Guid id);
    Task<(List<BonRetour> Items, int TotalCount)> GetAllAsync(int page, int size);
    Task<(List<BonRetour> Items, int TotalCount)> GetBySourceIdAsync(Guid sourceId, int page, int size);
    Task<(List<BonRetour> Items, int TotalCount)> GetByRetourSourceTypeAsync(RetourSourceType sourceType, int page, int size);
    Task<(List<BonRetour> Items, int TotalCount)> GetPagedDeletedAsync(int page, int size);
    Task<(List<BonRetour> Items, int TotalCount)> GetPagedBySourceAsync(Guid sourceId, int page, int size);
    Task<(List<BonRetour> Items, int TotalCount)> GetPagedByDateRangeAsync(DateTime from, DateTime to, int page, int size);
    Task<BonStatsDto> GetStatsAsync();
}