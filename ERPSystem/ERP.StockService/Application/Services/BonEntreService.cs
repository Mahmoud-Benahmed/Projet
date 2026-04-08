using ERP.StockService.Application.DTOs;
using ERP.StockService.Application.Exceptions;
using ERP.StockService.Application.Interfaces;
using ERP.StockService.Domain;
using ERP.StockService.Infrastructure.Messaging;

namespace ERP.StockService.Application.Services;

public class BonEntreService : IBonEntreService
{
    private readonly IBonEntreRepository _repo;
    private readonly IArticleServiceHttpClient _articleService;
    private readonly IBonNumeroRepository _bonNumberRepo;
    private readonly IJournalStockRepository _journalStockRepository;

    public BonEntreService(IBonEntreRepository repo, IArticleServiceHttpClient articleService, 
        IBonNumeroRepository bonNumberRepository, IJournalStockRepository journalStockRepository)
    {
        _repo = repo;
        _articleService = articleService;
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
            await _articleService.GetByIdAsync(l.ArticleId);
            bon.AddLigne(l.ArticleId, l.Quantity, l.Price);
        }

        bon.ValidateLignes();

        await _repo.AddAsync(bon);
        await _repo.SaveChangesAsync();

        foreach (var ligne in bon.Lignes)
        {
            var stockBefore = ligne.Quantity; // calculate the quantity by fetching the quantity from LigneEntre, LigneSortie, LigneRetour

            var journal = JournalStock.Create(
                ligne.ArticleId,
                ligne.Id,
                bon.Id,
                ligne.Quantity,
                stockBefore,
                StockMovementType.BonEntre,
                "StockService",
                "CreateBonEntre",
                userId
            );

            await _journalStockRepository.AddAsync(journal);
            await _journalStockRepository.SaveChangesAsync();
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

        if (dto.Lignes is { Count: > 0 })
        {
            bon.ClearLignes();
            foreach (var l in dto.Lignes)
            {
                await _articleService.GetByIdAsync(l.ArticleId);
                bon.AddLigne(l.ArticleId, l.Quantity, l.Price);  // Id = Guid.Empty → Added
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
                StockMovementType.BonEntre,
                "StockService",
                "CreateBonEntre",
                userId
            );

            await _journalStockRepository.AddAsync(journal);
            await _journalStockRepository.SaveChangesAsync();
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