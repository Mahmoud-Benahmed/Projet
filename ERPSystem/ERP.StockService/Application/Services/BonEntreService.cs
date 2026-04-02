using ERP.StockService.Application.DTOs;
using ERP.StockService.Application.Exceptions;
using ERP.StockService.Application.Interfaces;
using ERP.StockService.Domain;
using ERP.StockService.Infrastructure.Persistence.Messaging;
using static ERP.StockService.Properties.ApiRoutes;

namespace ERP.StockService.Application.Services;

public class BonEntreService : IBonEntreService
{
    private readonly IBonEntreRepository _repo;
    private readonly IFournisseurRepository _fournisseurRepo;
    private readonly IArticleService _articleService;
    private readonly IBonNumeroRepository _bonNumberRepo;

    public BonEntreService(IBonEntreRepository repo, IFournisseurRepository fournisseurRepo, 
                            IArticleService articleService, IBonNumeroRepository bonNumberRepository)
    {
        _repo = repo;
        _fournisseurRepo = fournisseurRepo;
        _articleService = articleService;
        _bonNumberRepo= bonNumberRepository;
    }

    // =========================
    // CREATE
    // =========================
    public async Task<BonEntreResponseDto> CreateAsync(CreateBonEntreRequestDto dto)
    {
        var fournisseur = await _fournisseurRepo.GetByIdAsync(dto.FournisseurId)
            ?? throw new FournisseurNotFoundException(dto.FournisseurId);

        if (fournisseur.IsBlocked)
            throw new FournisseurBlockedException(dto.FournisseurId);

        var numero = await _bonNumberRepo.GetNextDocumentNumberAsync("BON_ENTRE");
        var bon = BonEntre.Create(numero, fournisseur, dto.Observation);

        foreach (var l in dto.Lignes ?? [])
        {
            await _articleService.ExistsByIdAsync(l.ArticleId);
            bon.AddLigne(l.ArticleId, l.Quantity, l.Price);
        }

        bon.ValidateLignes();

        await _repo.AddAsync(bon);
        await _repo.SaveChangesAsync();
        return bon.ToResponseDto();
    }

    // =========================
    // UPDATE
    // =========================
    public async Task<BonEntreResponseDto> UpdateAsync(Guid id, UpdateBonEntreRequestDto dto)
    {
        var bon = await _repo.GetByIdAsync(id) ?? throw new BonEntreNotFoundException(id);
        var fournisseur = await _fournisseurRepo.GetByIdAsync(dto.FournisseurId)
            ?? throw new FournisseurNotFoundException(dto.FournisseurId);

        bon.Update(fournisseur, dto.Observation);

        if (dto.Lignes is { Count: > 0 })
        {
            bon.ClearLignes();
            foreach (var l in dto.Lignes)
            {
                await _articleService.ExistsByIdAsync(l.ArticleId);
                bon.AddLigne(l.ArticleId, l.Quantity, l.Price);  // Id = Guid.Empty → Added
            }
            bon.ValidateLignes();
        }

        await _repo.SaveChangesAsync();
        return bon.ToResponseDto();
    }

    // =========================
    // DELETE
    // =========================
    public async Task DeleteAsync(Guid id)
    {
        var bon = await _repo.GetByIdAsync(id) ?? throw new BonEntreNotFoundException(id);
        bon.Delete();
        await _repo.SaveChangesAsync();
    }

    // =========================
    // READ
    // =========================
    public async Task<BonEntreResponseDto> GetByIdAsync(Guid id)
    {
        var bon = await _repo.GetByIdAsync(id) ?? throw new BonEntreNotFoundException(id);
        return bon.ToResponseDto();
    }

    public async Task<PagedResultDto<BonEntreResponseDto>> GetAllAsync(int page, int size)
    {
        ValidatePaging(page, size);
        var (items, total) = await _repo.GetAllAsync(page, size);
        return new PagedResultDto<BonEntreResponseDto>(
            items.Select(b => b.ToResponseDto()).ToList(), total, page, size);
    }

    public async Task<PagedResultDto<BonEntreResponseDto>> GetPagedDeletedAsync(int page, int size)
    {
        ValidatePaging(page, size);
        var (items, total) = await _repo.GetPagedDeletedAsync(page, size);
        return new PagedResultDto<BonEntreResponseDto>(
            items.Select(b => b.ToResponseDto()).ToList(), total, page, size);
    }

    public async Task<PagedResultDto<BonEntreResponseDto>> GetPagedByFournisseurAsync(
        Guid fournisseurId, int page, int size)
    {
        ValidatePaging(page, size);
        var (items, total) = await _repo.GetByFournisseurAsync(fournisseurId, page, size);
        return new PagedResultDto<BonEntreResponseDto>(
            items.Select(b => b.ToResponseDto()).ToList(), total, page, size);
    }

    public async Task<PagedResultDto<BonEntreResponseDto>> GetPagedByDateRangeAsync(
        DateTime from, DateTime to, int page, int size)
    {
        ValidatePaging(page, size);

        if (from > to)
        {
            (from, to) = (to, from);
        }

        var (items, total) = await _repo.GetPagedByDateRangeAsync(from, to, page, size);
        return new PagedResultDto<BonEntreResponseDto>(
            items.Select(b => b.ToResponseDto()).ToList(), total, page, size);
    }

    public async Task<BonStatsDto> GetStatsAsync()
    {
        return await _repo.GetStatsAsync();
    }

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