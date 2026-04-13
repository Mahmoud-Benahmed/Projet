using ERP.StockService.Application.DTOs;
using ERP.StockService.Application.Exceptions;
using ERP.StockService.Application.Interfaces;
using ERP.StockService.Domain;
using ERP.StockService.Infrastructure.Messaging;
using ERP.StockService.Infrastructure.Persistence.Repositories;

namespace ERP.StockService.Application.Services;

public class BonSortieService : IBonSortieService
{
    private readonly IBonSortieRepository _repo;
    private readonly IArticleCacheRepository _articleCacheRepository;
    private readonly IClientCacheRepository _clientCacheRepository;
    private readonly IBonNumeroRepository _bonNumeroRepository;
    private readonly IJournalStockRepository _journalStockRepository;

    public BonSortieService(IBonSortieRepository repo, 
        IArticleCacheRepository articleCacheRepository,
        IClientCacheRepository clientCacheRepository, IBonNumeroRepository bonNumeroRepository,
        IJournalStockRepository journalStockRepository)
    {
        _repo = repo;
        _articleCacheRepository = articleCacheRepository;
        _clientCacheRepository = clientCacheRepository;
        _bonNumeroRepository = bonNumeroRepository;
        _journalStockRepository = journalStockRepository;
    }

    // =========================
    // CREATE
    // =========================
    public async Task<BonSortieResponseDto> CreateAsync(CreateBonSortieRequestDto dto, Guid requesterId)
    {
        var client = await _clientCacheRepository.GetByIdAsync(dto.ClientId) ?? throw new KeyNotFoundException($"Client with Id {dto.ClientId} not found");

        var numero = await _bonNumeroRepository.GetNextDocumentNumberAsync("BON_SORTIE");
        var bon = BonSortie.Create(numero, dto.ClientId, dto.Observation);

        foreach (var l in dto.Lignes ?? [])
        {
            //await _articleService.GetByIdAsync(l.ArticleId);
            bon.AddLigne(l.ArticleId, l.Quantity, l.Price);
        }

        bon.ValidateLignes();

        await _repo.AddAsync(bon);
        await _repo.SaveChangesAsync();

        foreach (var ligne in bon.Lignes)
        {
            var stockBefore = ligne.Quantity;

            var journal = JournalStock.Create(
                ligne.ArticleId,
                ligne.Id,
                bon.Id,
                ligne.Quantity,
                stockBefore,
                StockMovementType.BonEntre,
                "StockService",
                "CreateBonEntre",
                requesterId
            );

            await _journalStockRepository.AddAsync(journal);
            await _journalStockRepository.SaveChangesAsync();
        }
        return bon.ToResponseDto();
    }

    // =========================
    // UPDATE
    // =========================
    public async Task<BonSortieResponseDto> UpdateAsync(Guid id, UpdateBonSortieRequestDto dto, Guid requesterId)
    {
        var bon = await _repo.GetByIdAsync(id) ?? throw new BonSortieNotFoundException(id);
        var client = await _clientCacheRepository.GetByIdAsync(dto.ClientId) ?? throw new KeyNotFoundException($"Client with Id {dto.ClientId} not found");

        bon.Update(dto.ClientId, dto.Observation);

        if (dto.Lignes is { Count: > 0 })
        {
            bon.ClearLignes();
            foreach (var l in dto.Lignes)
            {
                decimal stockBefore = await _journalStockRepository.GetCurrentStockAsync(l.ArticleId);
                if (l.Quantity > stockBefore)
                    throw new InsufficientStockException(l.ArticleId, stockBefore, l.Quantity);

                //await _articleService.GetByIdAsync(l.ArticleId);
                bon.AddLigne(l.ArticleId, l.Quantity, l.Price);
            }
            bon.ValidateLignes();
        }
        await _repo.SaveChangesAsync();

        foreach (var ligne in bon.Lignes)
        {
            var stockBefore = ligne.Quantity;

            var journal = JournalStock.Create(
                ligne.ArticleId,
                ligne.Id,
                bon.Id,
                ligne.Quantity,
                stockBefore,
                StockMovementType.BonSortie,
                "StockService",
                "CreateBonSortie",
                requesterId
            );

            await _journalStockRepository.AddAsync(journal);
            await _journalStockRepository.SaveChangesAsync();
        }
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
        var client = await _clientCacheRepository.GetByIdAsync(clientId) ?? throw new KeyNotFoundException($"Client with Id {clientId} not found");

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