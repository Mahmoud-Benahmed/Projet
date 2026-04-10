using ERP.FournisseurService.Application.DTOs;

namespace ERP.FournisseurService.Application.Interfaces;

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
