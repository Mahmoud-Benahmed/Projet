using ERP.StockService.Application.DTOs;
using ERP.StockService.Application.Interfaces;

namespace ERP.StockService.Infrastructure.Messaging.Events.FournisseurEvents;

public sealed class FournisseurEventHandler : IFournisseurEventHandler
{
    private readonly IFournisseurCacheService _cacheService;
    private readonly ILogger<FournisseurEventHandler> _logger;

    public FournisseurEventHandler(
        IFournisseurCacheService cacheService,
        ILogger<FournisseurEventHandler> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public Task HandleCreatedAsync(FournisseurResponseDto dto)
        => _cacheService.SyncCreatedAsync(dto);

    public Task HandleUpdatedAsync(FournisseurResponseDto dto)
        => _cacheService.SyncUpdatedAsync(dto);

    public Task HandleDeletedAsync(FournisseurResponseDto dto)
        => _cacheService.SyncDeletedAsync(dto);

    public Task HandleRestoredAsync(FournisseurResponseDto dto)
        => _cacheService.SyncRestoredAsync(dto);
}