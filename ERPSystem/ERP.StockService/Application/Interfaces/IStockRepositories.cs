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


public interface IBonNumeroRepository
{
    /// <summary>
    /// Gets the next document number for the specified document type.
    /// This method should be called within a transaction to ensure uniqueness.
    /// </summary>
    Task<string> GetNextDocumentNumberAsync(string documentType);

    /// <summary>
    /// Gets the current sequence for a document type (for inspection).
    /// </summary>
    Task<BonNumber?> GetSequenceAsync(string documentType);
}

// IBonEntreRepository
public interface IBonEntreRepository
{
    Task AddAsync(BonEntre b);
    Task SaveChangesAsync();
    Task DeleteByIdAsync(Guid id);
    Task<BonEntre?> GetByIdAsync(Guid id);

    Task<(List<BonEntre> Items, int TotalCount)> GetAllAsync(int page, int size);
    Task<(List<BonEntre> Items, int TotalCount)> GetByFournisseurAsync(Guid fournisseurId, int page, int size);
    Task<(List<BonEntre> Items, int TotalCount)> GetPagedByDateRangeAsync(DateTime from, DateTime to, int page, int size);
    Task<BonStatsDto> GetStatsAsync();
}

public interface IBonSortieRepository
{
    Task AddAsync(BonSortie b);
    Task SaveChangesAsync();
    Task DeleteByIdAsync(Guid id);
    Task<BonSortie?> GetByIdAsync(Guid id);
    Task<(List<BonSortie> Items, int TotalCount)> GetAllAsync(int page, int size);
    Task<(List<BonSortie> Items, int TotalCount)> GetByClientAsync(Guid clientId, int page, int size);
    Task<(List<BonSortie> Items, int TotalCount)> GetPagedByDateRangeAsync(DateTime from, DateTime to, int page, int size);
    Task<(List<BonSortie> Items, int TotalCount)> GetPagedByClientAsync(Guid clientId, int page, int size);
    Task<BonStatsDto> GetStatsAsync();
}
public interface IBonRetourRepository
{
    Task AddAsync(BonRetour b);
    Task SaveChangesAsync();
    Task DeleteByIdAsync(Guid id);
    Task<BonRetour?> GetByIdAsync(Guid id);
    Task<(List<BonRetour> Items, int TotalCount)> GetAllAsync(int page, int size);
    Task<(List<BonRetour> Items, int TotalCount)> GetBySourceIdAsync(Guid sourceId, int page, int size);
    Task<(List<BonRetour> Items, int TotalCount)> GetByRetourSourceTypeAsync(RetourSourceType sourceType, int page, int size);
    Task<(List<BonRetour> Items, int TotalCount)> GetPagedBySourceAsync(Guid sourceId, int page, int size);
    Task<(List<BonRetour> Items, int TotalCount)> GetPagedByDateRangeAsync(DateTime from, DateTime to, int page, int size);
    Task<BonStatsDto> GetStatsAsync();
}