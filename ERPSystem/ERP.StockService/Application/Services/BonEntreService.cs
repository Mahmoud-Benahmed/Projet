using ERP.StockService.Application.DTOs;
using ERP.StockService.Application.Exceptions;
using ERP.StockService.Application.Interfaces;
using ERP.StockService.Domain;
using ERP.StockService.Infrastructure.Messaging;
using ERP.StockService.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using static ERP.StockService.Properties.ApiRoutes;

namespace ERP.StockService.Application.Services;

public class BonEntreService : IBonEntreService
{
    private readonly IBonEntreRepository _repo;
    private readonly IBonNumeroRepository _bonNumberRepo;
    private readonly IJournalStockRepository _journalStockRepository;

    public BonEntreService(IBonEntreRepository repo,
        IBonNumeroRepository bonNumberRepository, IJournalStockRepository journalStockRepository)
    {
        _repo = repo;
        //_articleService = articleService;
        _bonNumberRepo = bonNumberRepository;
        _journalStockRepository = journalStockRepository;
    }

    // =========================
    // CREATE
    // =========================
    public async Task<BonEntreResponseDto> CreateAsync(CreateBonEntreRequestDto dto, Guid userId)
    {

        var numero = await _bonNumberRepo.GetNextDocumentNumberAsync("BON_ENTRE");
        var bon = BonEntre.Create(numero, dto.FournisseurId, dto.Observation);

        foreach (var l in dto.Lignes ?? [])
        {
            //await _articleService.GetByIdAsync(l.ArticleId);
            bon.AddLigne(l.ArticleId, l.Quantity, l.Price);
        }

        bon.ValidateLignes();

        await using var transaction = await _repo.BeginTransactionAsync();

        try
        {
            // 1. Save BonEntre + lignes first
            await _repo.AddAsync(bon);
            await _repo.SaveChangesAsync();

            // 2. Create journal entries — all in one loop, NO SaveChanges inside
            foreach (var ligne in bon.Lignes)
            {
                decimal stockBefore = await _journalStockRepository
                                            .GetCurrentStockAsync(ligne.ArticleId);

                var journal = JournalStock.Create(
                    articleId: ligne.ArticleId,
                    ligneId: ligne.Id,
                    pieceId: bon.Id,
                    quantity: ligne.Quantity,   // renamed from quantity
                    stockBefore: stockBefore,
                    movementType: StockMovementType.BonEntre,
                    sourceService: "StockService",
                    sourceOperation: "CreateBonEntre",
                    performedBy: userId
                );

                await _journalStockRepository.AddAsync(journal);
            }

            // 3. ONE SaveChanges for all journals
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
    // UPDATE
    // =========================
    public async Task<BonEntreResponseDto> UpdateAsync(Guid id, UpdateBonEntreRequestDto dto, Guid userId)
    {
        var bon = await _repo.GetByIdAsync(id) ?? throw new BonEntreNotFoundException(id);

        bon.Update(dto.FournisseurId, dto.Observation);

        if (dto.Lignes is not { Count: > 0 })
        {
            await _repo.SaveChangesAsync();
            return bon.ToResponseDto();
        }

        // Capture old quantities BEFORE clearing
        var oldQtyMap = bon.Lignes
            .ToDictionary(l => l.ArticleId, l => l.Quantity);

        bon.ClearLignes();

        foreach (var l in dto.Lignes)
        {
            //await _articleService.GetByIdAsync(l.ArticleId);
            bon.AddLigne(l.ArticleId, l.Quantity, l.Price);
        }

        bon.ValidateLignes();

        await using var transaction = await _repo.BeginTransactionAsync();

        try
        {
            await _repo.SaveChangesAsync(); // persist new lignes, get their Ids

            var newQtyMap = bon.Lignes
                .ToDictionary(l => l.ArticleId, l => l.Quantity);

            // 1. Handle new or modified articles
            foreach (var ligne in bon.Lignes)
            {
                oldQtyMap.TryGetValue(ligne.ArticleId, out decimal oldQty);
                decimal delta = ligne.Quantity - oldQty;

                if (delta == 0) continue; // no stock movement needed

                decimal stockBefore = await _journalStockRepository
                                            .GetCurrentStockAsync(ligne.ArticleId);
                decimal stockAfter = stockBefore + delta;

                var journal = JournalStock.Create(
                    articleId: ligne.ArticleId,
                    ligneId: ligne.Id,
                    pieceId: bon.Id,
                    quantity: delta,           // delta, not full qty
                    stockBefore: stockBefore,
                    movementType: StockMovementType.BonEntre,
                    sourceService: "StockService",
                    sourceOperation: "UpdateBonEntre",
                    performedBy: userId
                );

                await _journalStockRepository.AddAsync(journal);
            }

            // 2. Handle removed articles (were in old, not in new) → reversal
            foreach (var (articleId, oldQty) in oldQtyMap)
            {
                if (newQtyMap.ContainsKey(articleId)) continue;

                decimal stockBefore = await _journalStockRepository
                                            .GetCurrentStockAsync(articleId);
                decimal delta = -oldQty; // negative = reversal
                decimal stockAfter = stockBefore + delta;

                var reversal = JournalStock.Create(
                    articleId: articleId,
                    ligneId: Guid.Empty,  // original ligne was deleted
                    pieceId: bon.Id,
                    quantity: delta,
                    stockBefore: stockBefore,
                    movementType: StockMovementType.BonEntre,
                    sourceService: "StockService",
                    sourceOperation: "UpdateBonEntre_Reversal",
                    performedBy: userId
                );

                await _journalStockRepository.AddAsync(reversal);
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
    // DELETE
    // =========================
    public async Task DeleteAsync(Guid id)
    {
        var bon = await _repo.GetByIdAsync(id) ?? throw new BonEntreNotFoundException(id);

        await _repo.DeleteByIdAsync(id);
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