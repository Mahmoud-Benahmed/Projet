using ERP.StockService.Application.DTOs;

namespace ERP.StockService.Application.Interfaces;

public interface IBonEntreService
{
    // ── Write ─────────────────────────────────────────────────────────────────
    Task<BonEntreResponseDto> CreateAsync(CreateBonEntreRequestDto dto, Guid userId);
    Task<BonEntreResponseDto> UpdateAsync(Guid id, UpdateBonEntreRequestDto dto, Guid userId);
    Task DeleteAsync(Guid id);

    // ── Read ──────────────────────────────────────────────────────────────────
    Task<BonEntreResponseDto> GetByIdAsync(Guid id);
    Task<PagedResultDto<BonEntreResponseDto>> GetAllAsync(int page, int size);
    Task<PagedResultDto<BonEntreResponseDto>> GetPagedByFournisseurAsync(Guid fournisseurId, int page, int size);
    Task<PagedResultDto<BonEntreResponseDto>> GetPagedByDateRangeAsync(DateTime from, DateTime to, int page, int size);
    Task<BonStatsDto> GetStatsAsync();

}

public interface IBonSortieService
{
    // ── Write ─────────────────────────────────────────────────────────────────
    Task<BonSortieResponseDto> CreateAsync(CreateBonSortieRequestDto dto, Guid requesterId);
    Task<BonSortieResponseDto> UpdateAsync(Guid id, UpdateBonSortieRequestDto dto, Guid requesterId);
    Task DeleteAsync(Guid id);

    // ── Read ──────────────────────────────────────────────────────────────────
    Task<BonSortieResponseDto> GetByIdAsync(Guid id);
    Task<PagedResultDto<BonSortieResponseDto>> GetAllAsync(int page, int size);
    Task<PagedResultDto<BonSortieResponseDto>> GetPagedByClientAsync(Guid clientId, int page, int size);
    Task<PagedResultDto<BonSortieResponseDto>> GetPagedByDateRangeAsync(DateTime from, DateTime to, int page, int size);
    Task<BonStatsDto> GetStatsAsync();
}

public interface IBonRetourService
{
    // ── Write ─────────────────────────────────────────────────────────────────
    Task<BonRetourResponseDto> CreateAsync(CreateBonRetourRequestDto dto, Guid requesterId);
    Task<BonRetourResponseDto> UpdateAsync(Guid id, UpdateBonRetourRequestDto dto, Guid requesterId);
    Task DeleteAsync(Guid id);

    // ── Read ──────────────────────────────────────────────────────────────────
    Task<BonRetourResponseDto> GetByIdAsync(Guid id);
    Task<PagedResultDto<BonRetourResponseDto>> GetAllAsync(int page, int size);
    Task<PagedResultDto<BonRetourResponseDto>> GetPagedBySourceAsync(Guid sourceId, int page, int size);
    Task<PagedResultDto<BonRetourResponseDto>> GetPagedByDateRangeAsync(DateTime from, DateTime to, int page, int size);
    Task<BonStatsDto> GetStatsAsync();

}