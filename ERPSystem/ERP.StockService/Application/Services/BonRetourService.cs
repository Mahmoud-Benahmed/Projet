using ERP.StockService.Application.DTOs;
using ERP.StockService.Application.Exceptions;
using ERP.StockService.Application.Interfaces;
using ERP.StockService.Domain;
using ERP.StockService.Infrastructure.Messaging;
using ERP.StockService.Infrastructure.Persistence.Repositories;

namespace ERP.StockService.Application.Services;

public class BonRetourService : IBonRetourService
{
    private readonly IBonRetourRepository _repo;
    private readonly IBonSortieRepository _bonSortieRepo;
    private readonly IBonEntreRepository _bonEntreRepo;
    private readonly IArticleServiceHttpClient _articleService;
    private readonly IClientServiceHttpClient _clientService;
    private readonly IBonNumeroRepository _bonNumeroRepository;
    private readonly IJournalStockRepository _journalStockRepository;


    public BonRetourService(
        IBonRetourRepository repo,
        IBonSortieRepository bonSortieRepo,
        IBonEntreRepository bonEntreRepo,
        IArticleServiceHttpClient articleService,
        IClientServiceHttpClient clientService,
        IBonNumeroRepository bonNumeroRepository,
        IJournalStockRepository journalStockRepository)
    {
        _repo = repo;
        _bonSortieRepo = bonSortieRepo;
        _bonEntreRepo = bonEntreRepo;
        _articleService = articleService;
        _clientService = clientService;
        _bonNumeroRepository = bonNumeroRepository;
        _journalStockRepository = journalStockRepository;
    }

    // =========================
    // CREATE
    // =========================
    public async Task<BonRetourResponseDto> CreateAsync(CreateBonRetourRequestDto dto, Guid requesterId)
    {
        // 1. Resolve source bon and validate party existence
        IReadOnlyList<LigneSource> sourceLignes = dto.SourceType switch
        {
            RetourSourceType.BonSortie => await ResolveBonSortieAsync(dto.SourceId),
            RetourSourceType.BonEntre => await ResolveBonEntreAsync(dto.SourceId),
            _ => throw new ArgumentOutOfRangeException(nameof(dto.SourceType))
        };
        var numero = await _bonNumeroRepository.GetNextDocumentNumberAsync("BON_RETOUR");
        var bon = BonRetour.Create(numero, dto.SourceId, dto.SourceType, dto.Motif, dto.Observation);

        // 2. Validate and add lignes
        foreach (var l in dto.Lignes ?? [])
        {
            await _articleService.GetByIdAsync(l.ArticleId);

            var sourceLigne = sourceLignes.FirstOrDefault(s => s.ArticleId == l.ArticleId)
                ?? throw new ArticleNotInSourceBonException(l.ArticleId, dto.SourceId);

            if (l.Quantity > sourceLigne.Quantity)
                throw new RetourQuantityExceedsSourceException(l.ArticleId, l.Quantity, sourceLigne.Quantity);

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
    public async Task<BonRetourResponseDto> UpdateAsync(Guid id, UpdateBonRetourRequestDto dto, Guid requesterId)
    {
        var bon = await _repo.GetByIdAsync(id) ?? throw new BonRetourNotFoundException(id);
        var bonEntre = await _bonEntreRepo.GetByIdAsync(dto.SourceId);
        var bonSortie = await _bonSortieRepo.GetByIdAsync(dto.SourceId);
        if (bonEntre is null && bonSortie is null)
            throw new BonNotFoundException(dto.SourceId);

        var sourceType = bonEntre is not null ? "BonEntre" : "BonSortie";

        bon.Update(dto.SourceId, sourceType, dto.Motif, dto.Observation);

        if (dto.Lignes is { Count: > 0 })
        {
            bon.ClearLignes();
            foreach (var l in dto.Lignes)
            {
                decimal stockBefore = await _journalStockRepository.GetCurrentStockAsync(l.ArticleId);
                if (l.Quantity > stockBefore)
                    throw new InsufficientStockException(l.ArticleId, stockBefore, l.Quantity);

                await _articleService.GetByIdAsync(l.ArticleId);
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
                StockMovementType.BonRetour,
                "StockService",
                "CreateBonRetour",
                requesterId
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
        var bon = await _repo.GetByIdAsync(id) ?? throw new BonRetourNotFoundException(id);

        await _repo.DeleteByIdAsync(id);
    }


    // =========================
    // READ
    // =========================
    public async Task<BonRetourResponseDto> GetByIdAsync(Guid id)
    {
        var bon = await _repo.GetByIdAsync(id) ?? throw new BonRetourNotFoundException(id);
        return bon.ToResponseDto();
    }

    public async Task<PagedResultDto<BonRetourResponseDto>> GetAllAsync(int page, int size)
    {
        ValidatePaging(page, size);
        var (items, total) = await _repo.GetAllAsync(page, size);
        return new PagedResultDto<BonRetourResponseDto>(
            items.Select(b => b.ToResponseDto()).ToList(), total, page, size);
    }

    public async Task<PagedResultDto<BonRetourResponseDto>> GetPagedBySourceAsync(
        Guid sourceId, int page, int size)
    {
        ValidatePaging(page, size);
        var (items, total) = await _repo.GetPagedBySourceAsync(sourceId, page, size);
        return new PagedResultDto<BonRetourResponseDto>(
            items.Select(b => b.ToResponseDto()).ToList(), total, page, size);
    }

    public async Task<PagedResultDto<BonRetourResponseDto>> GetPagedByDateRangeAsync(
        DateTime from, DateTime to, int page, int size)
    {
        ValidatePaging(page, size);
        // Swap dates if needed
        if (from > to)
        {
            (from, to) = (to, from);
        }

        var (items, total) = await _repo.GetPagedByDateRangeAsync(from, to, page, size);
        return new PagedResultDto<BonRetourResponseDto>(
            items.Select(b => b.ToResponseDto()).ToList(), total, page, size);
    }

    // =========================
    // HELPERS
    // =========================
    private async Task<IReadOnlyList<LigneSource>> ResolveBonSortieAsync(Guid sourceId)
    {
        var bonSortie = await _bonSortieRepo.GetByIdAsync(sourceId)
            ?? throw new BonSortieNotFoundException(sourceId);
        await _clientService.GetByIdAsync(bonSortie.ClientId);
        return bonSortie.Lignes.Select(l => new LigneSource(l.ArticleId, l.Quantity)).ToList();
    }

    private async Task<IReadOnlyList<LigneSource>> ResolveBonEntreAsync(Guid sourceId)
    {
        var bonEntre = await _bonEntreRepo.GetByIdAsync(sourceId)
            ?? throw new BonEntreNotFoundException(sourceId);
        return bonEntre.Lignes.Select(l => new LigneSource(l.ArticleId, l.Quantity)).ToList();
    }

    private async Task<IReadOnlyList<LigneSource>> ResolveBonSortieSourceLignesAsync(Guid sourceId)
    {
        var bonSortie = await _bonSortieRepo.GetByIdAsync(sourceId)
            ?? throw new BonSortieNotFoundException(sourceId);
        return bonSortie.Lignes.Select(l => new LigneSource(l.ArticleId, l.Quantity)).ToList();
    }

    private async Task<IReadOnlyList<LigneSource>> ResolveBonEntreSourceLignesAsync(Guid sourceId)
    {
        var bonEntre = await _bonEntreRepo.GetByIdAsync(sourceId)
            ?? throw new BonEntreNotFoundException(sourceId);
        return bonEntre.Lignes.Select(l => new LigneSource(l.ArticleId, l.Quantity)).ToList();
    }
    public async Task<BonStatsDto> GetStatsAsync()
    {
        return await _repo.GetStatsAsync();
    }


    private static void ValidatePaging(int page, int size)
    {
        if (page < 1) throw new ArgumentOutOfRangeException(nameof(page),
            "Page number must be greater than zero.");
        if (size < 1) throw new ArgumentOutOfRangeException(nameof(size),
            "Page size must be greater than zero.");
    }

    // Internal projection to avoid domain leakage
    private record LigneSource(Guid ArticleId, decimal Quantity);
}