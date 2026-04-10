using ERP.FournisseurService.Application.DTOs;
using ERP.FournisseurService.Application.Interfaces;
using ERP.FournisseurService.Domain;
using ERP.FournisseurService.Application.Exceptions;

namespace ERP.FournisseurService.Application.Services;

public class FournisseurService : IFournisseurService
{
    private readonly IFournisseurRepository _repo;

    public FournisseurService(IFournisseurRepository repo) => _repo = repo;

    // =========================
    // CREATE
    // =========================
    public async Task<FournisseurResponseDto> CreateAsync(CreateFournisseurRequestDto dto)
    {
        var f = Fournisseur.Create(
            dto.Name, dto.Address, dto.Phone,
            dto.TaxNumber, dto.RIB, dto.Email);
        await _repo.AddAsync(f);
        await _repo.SaveChangesAsync();
        return f.ToResponseDto();
    }

    // =========================
    // UPDATE
    // =========================
    public async Task<FournisseurResponseDto> UpdateAsync(Guid id, UpdateFournisseurRequestDto dto)
    {
        var f = await _repo.GetByIdAsync(id) ?? throw new FournisseurNotFoundException(id);
        f.Update(dto.Name, dto.Address, dto.Phone, dto.TaxNumber, dto.RIB, dto.Email);
        await _repo.SaveChangesAsync();
        return f.ToResponseDto();
    }

    // =========================
    // DELETE / RESTORE
    // =========================
    public async Task DeleteAsync(Guid id)
    {
        var f = await _repo.GetByIdAsync(id) ?? throw new FournisseurNotFoundException(id);
        f.Delete();
        await _repo.SaveChangesAsync();
    }

    public async Task RestoreAsync(Guid id)
    {
        var f = await _repo.GetByIdDeletedAsync(id) ?? throw new FournisseurNotFoundException(id);
        if (!f.IsDeleted) return;
        f.Restore();
        await _repo.SaveChangesAsync();
    }

    // =========================
    // BLOCK / UNBLOCK
    // =========================
    public async Task<FournisseurResponseDto> BlockAsync(Guid id)
    {
        var f = await _repo.GetByIdAsync(id) ?? throw new FournisseurNotFoundException(id);
        f.Block();
        await _repo.SaveChangesAsync();
        return f.ToResponseDto();
    }

    public async Task<FournisseurResponseDto> UnblockAsync(Guid id)
    {
        var f = await _repo.GetByIdAsync(id) ?? throw new FournisseurNotFoundException(id);
        f.Unblock();
        await _repo.SaveChangesAsync();
        return f.ToResponseDto();
    }

    // =========================
    // READ
    // =========================
    public async Task<FournisseurResponseDto> GetByIdAsync(Guid id)
    {
        var f = await _repo.GetByIdAsync(id) ?? throw new FournisseurNotFoundException(id);
        return f.ToResponseDto();
    }

    public async Task<PagedResultDto<FournisseurResponseDto>> GetAllAsync(int page, int size)
    {
        ValidatePaging(page, size);
        var (items, total) = await _repo.GetAllAsync(page, size);
        return new PagedResultDto<FournisseurResponseDto>(
            items.Select(f => f.ToResponseDto()).ToList(), total, page, size);
    }

    public async Task<PagedResultDto<FournisseurResponseDto>> GetPagedDeletedAsync(int page, int size)
    {
        ValidatePaging(page, size);
        var (items, total) = await _repo.GetPagedDeletedAsync(page, size);
        return new PagedResultDto<FournisseurResponseDto>(
            items.Select(f => f.ToResponseDto()).ToList(), total, page, size);
    }

    public async Task<PagedResultDto<FournisseurResponseDto>> GetPagedByNameAsync(
        string nameFilter, int page, int size)
    {
        ValidatePaging(page, size);
        if (string.IsNullOrWhiteSpace(nameFilter))
            throw new ArgumentException("Name filter cannot be empty.", nameof(nameFilter));

        var (items, total) = await _repo.GetPagedByNameAsync(nameFilter, page, size);
        return new PagedResultDto<FournisseurResponseDto>(
            items.Select(f => f.ToResponseDto()).ToList(), total, page, size);
    }

    // =========================
    // STATS
    // =========================
    public async Task<FournisseurStatsDto> GetStatsAsync() =>
        await _repo.GetStatsAsync();

    // =========================
    // HELPERS
    // =========================
    private static void ValidatePaging(int page, int size)
    {
        if (page < 1) throw new ArgumentOutOfRangeException(nameof(page),
            "Page number must be greater than zero.");
        if (size < 1) throw new ArgumentOutOfRangeException(nameof(size),
            "Page size must be greater than zero.");
    }
}