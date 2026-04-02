using ERP.StockService.Application.DTOs;
using ERP.StockService.Domain;

namespace ERP.StockService.Application.Interfaces;

public interface IFournisseurService
{
    // ── Write ─────────────────────────────────────────────────────────────────
    Task<FournisseurResponseDto> CreateAsync(CreateFournisseurRequestDto dto);
    Task<FournisseurResponseDto> UpdateAsync(Guid id, UpdateFournisseurRequestDto dto);
    Task DeleteAsync(Guid id);
    Task RestoreAsync(Guid id);
    Task<FournisseurResponseDto> BlockAsync(Guid id);
    Task<FournisseurResponseDto> UnblockAsync(Guid id);

    // ── Read ──────────────────────────────────────────────────────────────────
    Task<FournisseurResponseDto> GetByIdAsync(Guid id);
    Task<PagedResultDto<FournisseurResponseDto>> GetAllAsync(int page, int size);
    Task<PagedResultDto<FournisseurResponseDto>> GetPagedDeletedAsync(int page, int size);
    Task<PagedResultDto<FournisseurResponseDto>> GetPagedByNameAsync(string nameFilter, int page, int size);

    // ── Stats ─────────────────────────────────────────────────────────────────
    Task<FournisseurStatsDto> GetStatsAsync();
}

public interface IBonEntreService
{
    // ── Write ─────────────────────────────────────────────────────────────────
    Task<BonEntreResponseDto> CreateAsync(CreateBonEntreRequestDto dto);
    Task<BonEntreResponseDto> UpdateAsync(Guid id, UpdateBonEntreRequestDto dto);
    Task DeleteAsync(Guid id);

    // ── Read ──────────────────────────────────────────────────────────────────
    Task<BonEntreResponseDto> GetByIdAsync(Guid id);
    Task<PagedResultDto<BonEntreResponseDto>> GetAllAsync(int page, int size);
    Task<PagedResultDto<BonEntreResponseDto>> GetPagedDeletedAsync(int page, int size);
    Task<PagedResultDto<BonEntreResponseDto>> GetPagedByFournisseurAsync(Guid fournisseurId, int page, int size);
    Task<PagedResultDto<BonEntreResponseDto>> GetPagedByDateRangeAsync(DateTime from, DateTime to, int page, int size);
    Task<BonStatsDto> GetStatsAsync();

}

public interface IBonSortieService
{
    // ── Write ─────────────────────────────────────────────────────────────────
    Task<BonSortieResponseDto> CreateAsync(CreateBonSortieRequestDto dto);
    Task<BonSortieResponseDto> UpdateAsync(Guid id, UpdateBonSortieRequestDto dto);
    Task DeleteAsync(Guid id);

    // ── Read ──────────────────────────────────────────────────────────────────
    Task<BonSortieResponseDto> GetByIdAsync(Guid id);
    Task<PagedResultDto<BonSortieResponseDto>> GetAllAsync(int page, int size);
    Task<PagedResultDto<BonSortieResponseDto>> GetPagedDeletedAsync(int page, int size);
    Task<PagedResultDto<BonSortieResponseDto>> GetPagedByClientAsync(Guid clientId, int page, int size);
    Task<PagedResultDto<BonSortieResponseDto>> GetPagedByDateRangeAsync(DateTime from, DateTime to, int page, int size);
    Task<BonStatsDto> GetStatsAsync();
}

public interface IBonRetourService
{
    // ── Write ─────────────────────────────────────────────────────────────────
    Task<BonRetourResponseDto> CreateAsync(CreateBonRetourRequestDto dto);
    Task<BonRetourResponseDto> UpdateAsync(Guid id, UpdateBonRetourRequestDto dto);
    Task DeleteAsync(Guid id);

    // ── Read ──────────────────────────────────────────────────────────────────
    Task<BonRetourResponseDto> GetByIdAsync(Guid id);
    Task<PagedResultDto<BonRetourResponseDto>> GetAllAsync(int page, int size);
    Task<PagedResultDto<BonRetourResponseDto>> GetPagedDeletedAsync(int page, int size);
    Task<PagedResultDto<BonRetourResponseDto>> GetPagedBySourceAsync(Guid sourceId, int page, int size);
    Task<PagedResultDto<BonRetourResponseDto>> GetPagedByDateRangeAsync(DateTime from, DateTime to, int page, int size);
    Task<BonStatsDto> GetStatsAsync();

}