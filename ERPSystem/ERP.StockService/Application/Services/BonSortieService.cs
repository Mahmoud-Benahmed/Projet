using ERP.StockService.Application.DTOs;
using ERP.StockService.Application.Exceptions;
using ERP.StockService.Application.Interfaces;
using ERP.StockService.Infrastructure.Messaging;

namespace ERP.StockService.Application.Services;

public class BonSortieService : IBonSortieService
{
    private readonly IBonSortieRepository _repo;
    private readonly IArticleService _articleService;
    private readonly IClientService _clientService;
    private readonly IBonNumeroRepository _bonNumeroRepository;

    public BonSortieService(IBonSortieRepository repo, IArticleService articleService, 
                            IClientService clientService, IBonNumeroRepository bonNumeroRepository)
    {
        _repo = repo;
        _clientService = clientService;
        _articleService = articleService;
        _bonNumeroRepository = bonNumeroRepository;
    }

    // =========================
    // CREATE
    // =========================
    public async Task<BonSortieResponseDto> CreateAsync(CreateBonSortieRequestDto dto)
    {
        await _clientService.ExistsByIdAsync(dto.ClientId);

        var numero = await _bonNumeroRepository.GetNextDocumentNumberAsync("BON_SORTIE");
        var bon = BonSortie.Create(numero, dto.ClientId, dto.Observation);

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
    public async Task<BonSortieResponseDto> UpdateAsync(Guid id, UpdateBonSortieRequestDto dto)
    {
        var bon = await _repo.GetByIdAsync(id) ?? throw new BonSortieNotFoundException(id);
        await _clientService.ExistsByIdAsync(dto.ClientId);

        bon.Update(dto.ClientId, dto.Observation);

        if (dto.Lignes is { Count: > 0 })
        {
            bon.ClearLignes();
            foreach (var l in dto.Lignes)
            {
                await _articleService.ExistsByIdAsync(l.ArticleId);
                bon.AddLigne(l.ArticleId, l.Quantity, l.Price);
            }
            bon.ValidateLignes();
        }

        await _repo.SaveChangesAsync();
        return bon.ToResponseDto();
    }

    // =========================
    // DELETE / RESTORE
    // =========================
    public async Task DeleteAsync(Guid id)
    {
        var bon = await _repo.GetByIdAsync(id) ?? throw new BonSortieNotFoundException(id);
        await _repo.DeleteByIdAsync(id);
    }

    // =========================
    // READ
    // =========================
    public async Task<BonSortieResponseDto> GetByIdAsync(Guid id)
    {
        var bon = await _repo.GetByIdAsync(id) ?? throw new BonSortieNotFoundException(id);
        return bon.ToResponseDto();
    }

    public async Task<PagedResultDto<BonSortieResponseDto>> GetAllAsync(int page, int size)
    {
        ValidatePaging(page, size);
        var (items, total) = await _repo.GetAllAsync(page, size);
        return new PagedResultDto<BonSortieResponseDto>(
            items.Select(b => b.ToResponseDto()).ToList(), total, page, size);
    }

    public async Task<PagedResultDto<BonSortieResponseDto>> GetPagedByClientAsync(
        Guid clientId, int page, int size)
    {
        ValidatePaging(page, size);
        await _clientService.ExistsByIdAsync(clientId);

        var (items, total) = await _repo.GetPagedByClientAsync(clientId, page, size);
        return new PagedResultDto<BonSortieResponseDto>(
            items.Select(b => b.ToResponseDto()).ToList(), total, page, size);
    }

    public async Task<PagedResultDto<BonSortieResponseDto>> GetPagedByDateRangeAsync(
        DateTime from, DateTime to, int page, int size)
    {
        ValidatePaging(page, size);
        if (from > to)
            (from, to) = (to, from);

        var (items, total) = await _repo.GetPagedByDateRangeAsync(from, to, page, size);
        return new PagedResultDto<BonSortieResponseDto>(
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