using ERP.StockService.Application.DTOs;
using ERP.StockService.Application.Exceptions;
using ERP.StockService.Application.Interfaces;
using ERP.StockService.Domain;
using System.Runtime.InteropServices;

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
        IClientCacheRepository clientCacheRepository, 
        IBonNumeroRepository bonNumeroRepository,
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
    public async Task<BonSortieResponseDto> CreateAsync(CreateBonSortieRequestDto dto)
    {
        _ = await _clientCacheRepository.GetByIdAsync(dto.ClientId) 
            ?? throw new KeyNotFoundException($"Client with Id {dto.ClientId} not found");

        if (dto.Lignes is null or { Count: 0 })
            throw new ArgumentException("At least one ligne is required.");

        var articleIds = dto.Lignes.Select(l => l.ArticleId).Distinct().ToList();
        var articles = await _articleCacheRepository.GetByIdsAsync(articleIds);

        var foundIds = articles.Select(a => a.Id).ToHashSet();
        var missingIds = articleIds.Where(id => !foundIds.Contains(id)).ToList();
        if (missingIds.Count != 0)
            throw new InvalidOperationException(
                $"Articles not found: {string.Join(", ", missingIds)}");

        await using var transaction = await _repo.BeginTransactionAsync();

        try
        {
            var numero = await _bonNumeroRepository.GetNextDocumentNumberAsync("BON_SORTIE");
            var bon = BonSortie.Create(numero, dto.ClientId, dto.Observation);

            var articleDictionary = articles.ToDictionary(a => a.Id, a => a);


            foreach (var l in dto.Lignes)
            {
               bon.AddLigne(l.ArticleId, l.Quantity, l.Price);
            }

            bon.ValidateLignes();

            await _repo.AddAsync(bon);
            await _repo.SaveChangesAsync();

            var stockMap = await _journalStockRepository
                                .GetCurrentStocksAsync(bon.Lignes.Select(l => l.ArticleId));

            foreach (var ligne in bon.Lignes)
            {
                var stockBefore = stockMap.GetValueOrDefault(ligne.ArticleId, 0);

                await _journalStockRepository.AddAsync(JournalStock.Create(
                    ligne.ArticleId,
                    ligne.Id,
                    bon.Id,
                    -ligne.Quantity,
                    stockBefore,
                    StockMovementType.BonSortie,
                    "StockService",
                    "CreateBonSortie"
                ));
            }

            await _journalStockRepository.SaveChangesAsync();
            await transaction.CommitAsync();
            return bon.ToResponseDto();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // =========================
    // UPDATE
    // =========================
    public async Task<BonSortieResponseDto> UpdateAsync(Guid id, UpdateBonSortieRequestDto dto)
    {
        var bon = await _repo.GetByIdAsync(id) ?? throw new BonSortieNotFoundException(id);
        _ = await _clientCacheRepository.GetByIdAsync(dto.ClientId) ?? throw new KeyNotFoundException($"Client with Id {dto.ClientId} not found");

        Dictionary<Guid, decimal> oldQtyMap = [];
        Dictionary<Guid, decimal> newQtyMap = [];

        if (dto.Lignes is not null)
        {
            var articleIds = dto.Lignes.Select(l => l.ArticleId).Distinct().ToList();
            var articles = await _articleCacheRepository.GetByIdsAsync(articleIds);

            var foundIds = articles.Select(a => a.Id).ToHashSet();
            var missingIds = articleIds.Where(id => !foundIds.Contains(id)).ToList();
            if (missingIds.Count != 0)
                throw new InvalidOperationException(
                    $"Articles not found: {string.Join(", ", missingIds)}");

            // Capture BEFORE mutating
            oldQtyMap = bon.Lignes
                .GroupBy(l => l.ArticleId)
                .ToDictionary(g => g.Key, g => g.Sum(l => l.Quantity));

            bon.ClearLignes();
            foreach (var l in dto.Lignes)
                bon.AddLigne(l.ArticleId, l.Quantity, l.Price);

            bon.ValidateLignes();
        }

        bon.Update(dto.ClientId, dto.Observation);

        await using var transaction = await _repo.BeginTransactionAsync();

        try
        {
            await _repo.SaveChangesAsync();

            // Populate AFTER SaveChanges so ligne IDs are materialized
            newQtyMap = bon.Lignes
                .GroupBy(l => l.ArticleId)
                .ToDictionary(g => g.Key, g => g.Sum(l => l.Quantity));

            foreach (var ligne in bon.Lignes)
            {
                oldQtyMap.TryGetValue(ligne.ArticleId, out decimal oldQty);
                decimal delta = ligne.Quantity - oldQty;
                if (delta == 0) continue;

                decimal stockBefore = await _journalStockRepository
                    .GetCurrentStockAsync(ligne.ArticleId);
                await _journalStockRepository.AddAsync(JournalStock.Create(
                    ligne.ArticleId,
                    ligne.Id,
                    bon.Id,
                    delta,
                    stockBefore,
                    StockMovementType.BonSortie,
                    "StockService",
                    "UpdateBonSortie"
                ));
            }

            foreach (var (articleId, oldQty) in oldQtyMap)
            {
                if (newQtyMap.ContainsKey(articleId)) continue;

                decimal stockBefore = await _journalStockRepository
                    .GetCurrentStockAsync(articleId);

                await _journalStockRepository.AddAsync(JournalStock.Create(
                    articleId: articleId,
                    ligneId: Guid.Empty,
                    pieceId: bon.Id,
                    quantity: -oldQty,
                    stockBefore: stockBefore,
                    movementType: StockMovementType.BonSortie,
                    sourceService: "StockService",
                    sourceOperation: "UpdateBonSortie_Reversal"
                ));

            }

            await _journalStockRepository.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        return bon.ToResponseDto();

    }

    // =========================
    // DELETE / RESTORE
    // =========================
    public async Task DeleteAsync(Guid id)
    {
        var bon = await _repo.GetByIdAsync(id) ?? throw new BonSortieNotFoundException(id);
        await using var transaction = await _repo.BeginTransactionAsync();
        try
        {
            foreach (var ligne in bon.Lignes)
            {
                decimal stockBefore = await _journalStockRepository.GetCurrentStockAsync(ligne.ArticleId);
                var reversal = JournalStock.Create(
                    articleId: ligne.ArticleId,
                    ligneId: ligne.Id,
                    pieceId: bon.Id,
                    quantity: -ligne.Quantity,
                    stockBefore: stockBefore,
                    movementType: StockMovementType.BonSortie,
                    sourceService: "StockService",
                    sourceOperation: "DeleteBonSortie"
                );
                await _journalStockRepository.AddAsync(reversal);
            }
            await _journalStockRepository.SaveChangesAsync();
            await _repo.DeleteByIdAsync(id);
            await _repo.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch { await transaction.RollbackAsync(); throw; }
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