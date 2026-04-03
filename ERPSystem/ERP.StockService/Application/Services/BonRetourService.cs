using ERP.StockService.Application.DTOs;
using ERP.StockService.Application.Exceptions;
using ERP.StockService.Application.Interfaces;
using ERP.StockService.Infrastructure.Messaging;

namespace ERP.StockService.Application.Services;

public class BonRetourService : IBonRetourService
{
    private readonly IBonRetourRepository _repo;
    private readonly IBonSortieRepository _bonSortieRepo;
    private readonly IBonEntreRepository _bonEntreRepo;
    private readonly IArticleService _articleService;
    private readonly IClientService _clientService;
    private readonly IFournisseurRepository _fournisseurRepo;
    private readonly IBonNumeroRepository _bonNumeroRepository;


    public BonRetourService(
        IBonRetourRepository repo,
        IBonSortieRepository bonSortieRepo,
        IBonEntreRepository bonEntreRepo,
        IArticleService articleService,
        IClientService clientService,
        IFournisseurRepository fournisseurRepo,
        IBonNumeroRepository bonNumeroRepository)
    {
        _repo = repo;
        _bonSortieRepo = bonSortieRepo;
        _bonEntreRepo = bonEntreRepo;
        _articleService = articleService;
        _clientService = clientService;
        _fournisseurRepo = fournisseurRepo;
        _bonNumeroRepository = bonNumeroRepository;
    }

    // =========================
    // CREATE
    // =========================
    public async Task<BonRetourResponseDto> CreateAsync(CreateBonRetourRequestDto dto)
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
            await _articleService.ExistsByIdAsync(l.ArticleId);

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
    public async Task<BonRetourResponseDto> UpdateAsync(Guid id, UpdateBonRetourRequestDto dto)
    {
        var bon = await _repo.GetByIdAsync(id) ?? throw new BonRetourNotFoundException(id);
        var bonEntre = await _bonEntreRepo.GetByIdAsync(dto.SourceId);
        var bonSortie = await _bonSortieRepo.GetByIdAsync(dto.SourceId);
        if (bonEntre is null && bonSortie is null)
            throw new BonNotFoundException(dto.SourceId);

        var sourceType = bonEntre is not null ? "BonEntre" : "BonSortie";

        bon.Update(dto.SourceId, sourceType ,dto.Motif, dto.Observation);
        
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
    // DELETE
    // =========================
    public async Task DeleteAsync(Guid id)
    {
        var bon = await _repo.GetByIdAsync(id) ?? throw new BonRetourNotFoundException(id);
        bon.Delete();
        await _repo.SaveChangesAsync();
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

    public async Task<PagedResultDto<BonRetourResponseDto>> GetPagedDeletedAsync(int page, int size)
    {
        ValidatePaging(page, size);
        var (items, total) = await _repo.GetPagedDeletedAsync(page, size);
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
        await _clientService.ExistsByIdAsync(bonSortie.ClientId);
        return bonSortie.Lignes.Select(l => new LigneSource(l.ArticleId, l.Quantity)).ToList();
    }

    private async Task<IReadOnlyList<LigneSource>> ResolveBonEntreAsync(Guid sourceId)
    {
        var bonEntre = await _bonEntreRepo.GetByIdAsync(sourceId)
            ?? throw new BonEntreNotFoundException(sourceId);
        var fournisseur = await _fournisseurRepo.GetByIdAsync(bonEntre.FournisseurId)
            ?? throw new FournisseurNotFoundException(bonEntre.FournisseurId);
        if (fournisseur.IsBlocked)
            throw new FournisseurBlockedException(bonEntre.FournisseurId);
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