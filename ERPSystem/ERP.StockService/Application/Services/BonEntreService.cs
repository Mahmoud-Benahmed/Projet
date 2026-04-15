using ERP.StockService.Application.DTOs;
using ERP.StockService.Application.Exceptions;
using ERP.StockService.Application.Interfaces;
using ERP.StockService.Domain;


namespace ERP.StockService.Application.Services;

public class BonEntreService : IBonEntreService
{
    private readonly IBonEntreRepository _repo;
    private readonly IBonNumeroRepository _bonNumberRepo;
    private readonly IJournalStockRepository _journalStockRepository;
    private readonly IFournisseurCacheRepository _fournisseurCacheRepository;
    private readonly IArticleCacheRepository _articleCacheRepository;

    public BonEntreService(
        IBonEntreRepository repo,
        IArticleCacheRepository articleCacheRepository,
        IBonNumeroRepository bonNumberRepository, 
        IJournalStockRepository journalStockRepository,
        IFournisseurCacheRepository fornisseurCacheRepo)
    {
        _repo = repo;
        _fournisseurCacheRepository = fornisseurCacheRepo;
        _bonNumberRepo = bonNumberRepository;
        _journalStockRepository = journalStockRepository;
        _articleCacheRepository = articleCacheRepository;
    }

    // =========================
    // CREATE
    // =========================
    public async Task<BonEntreResponseDto> CreateAsync(CreateBonEntreRequestDto dto)
    {
        _ = await _fournisseurCacheRepository.GetByIdAsync(dto.FournisseurId)
            ?? throw new KeyNotFoundException($"Fournisseur with Id:{dto.FournisseurId} not found.");

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
            var numero = await _bonNumberRepo.GetNextDocumentNumberAsync("BON_ENTRE");
            var bon = BonEntre.Create(numero, dto.FournisseurId, dto.Observation);

            foreach (var l in dto.Lignes)
                bon.AddLigne(l.ArticleId, l.Quantity, l.Price);

            bon.ValidateLignes();

            await _repo.AddAsync(bon);
            await _repo.SaveChangesAsync();

            var stockMap = await _journalStockRepository
                .GetCurrentStocksAsync(bon.Lignes.Select(l => l.ArticleId));

            foreach (var ligne in bon.Lignes)
            {
                decimal stockBefore = stockMap.GetValueOrDefault(ligne.ArticleId, 0);

                await _journalStockRepository.AddAsync(JournalStock.Create(
                    articleId: ligne.ArticleId,
                    ligneId: ligne.Id,
                    pieceId: bon.Id,
                    quantity: ligne.Quantity,
                    stockBefore: stockBefore,
                    movementType: StockMovementType.BonEntre,
                    sourceService: "StockService",
                    sourceOperation: "CreateBonEntre"
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
    public async Task<BonEntreResponseDto> UpdateAsync(Guid id, UpdateBonEntreRequestDto dto)
    {
        _ = await _fournisseurCacheRepository.GetByIdAsync(dto.FournisseurId)
            ?? throw new KeyNotFoundException($"Fournisseur with Id:{dto.FournisseurId} not found.");

        var bon = await _repo.GetByIdAsync(id)
            ?? throw new BonEntreNotFoundException(id);

        Dictionary<Guid, decimal> oldQtyMap = [];
        Dictionary<Guid, decimal> newQtyMap = [];

        // Only process lines if they were provided
        if (dto.Lignes is not null)
        {
            var articleIds = dto.Lignes.Select(l => l.ArticleId).Distinct().ToList();
            var articles = await _articleCacheRepository.GetByIdsAsync(articleIds);

            var foundIds = articles.Select(a => a.Id).ToHashSet();
            var missingIds = articleIds.Where(id => !foundIds.Contains(id)).ToList();
            if (missingIds.Count != 0)
                throw new InvalidOperationException(
                    $"Articles not found: {string.Join(", ", missingIds)}");

            // Capture old quantities before clearing
            oldQtyMap = bon.Lignes
                .GroupBy(l => l.ArticleId)
                .ToDictionary(g => g.Key, g => g.Sum(l => l.Quantity));

            bon.ClearLignes();
            foreach (var l in dto.Lignes)
                bon.AddLigne(l.ArticleId, l.Quantity, l.Price);

            bon.ValidateLignes();
        }

        bon.Update(dto.FournisseurId, dto.Observation);

        await using var transaction = await _repo.BeginTransactionAsync();
        try
        {
            await _repo.SaveChangesAsync();

            // Only create journal entries if lines were changed
            if (dto.Lignes is not null)
            {
                // Populate new quantities after SaveChanges
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
                        articleId: ligne.ArticleId,
                        ligneId: ligne.Id,
                        pieceId: bon.Id,
                        quantity: delta,
                        stockBefore: stockBefore,
                        movementType: StockMovementType.BonEntre,
                        sourceService: "StockService",
                        sourceOperation: "UpdateBonEntre"
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
                        movementType: StockMovementType.BonEntre,
                        sourceService: "StockService",
                        sourceOperation: "UpdateBonEntre_Reversal"
                    ));
                }

                await _journalStockRepository.SaveChangesAsync();
            }

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
    // DELETE
    // =========================
    public async Task DeleteAsync(Guid id)
    {
        var bon = await _repo.GetByIdAsync(id) ?? throw new BonEntreNotFoundException(id);

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
                    movementType: StockMovementType.BonEntre,
                    sourceService: "StockService",
                    sourceOperation: "DeleteBonEntre"
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