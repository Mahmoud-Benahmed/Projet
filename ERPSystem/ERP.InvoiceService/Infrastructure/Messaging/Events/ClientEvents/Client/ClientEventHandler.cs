using ERP.InvoiceService.Application.DTOs;
using ERP.InvoiceService.Application.Interfaces;

namespace ERP.InvoiceService.Infrastructure.Messaging.Events.ClientEvents.Client;

public sealed class ClientEventHandler : IClientEventHandler
{
    private readonly IClientCacheService _cacheService;

    public ClientEventHandler(IClientCacheService cacheService)
    {
        _cacheService = cacheService;
    }

    public Task HandleCreatedAsync(ClientResponseDto dto)
        => _cacheService.SyncCreatedAsync(dto);

    public Task HandleUpdatedAsync(ClientResponseDto dto)
        => _cacheService.SyncUpdatedAsync(dto);

    public Task HandleDeletedAsync(ClientResponseDto dto)
        => _cacheService.SyncDeletedAsync(dto);

    public Task HandleRestoredAsync(ClientResponseDto dto)
        => _cacheService.SyncRestoredAsync(dto);
}