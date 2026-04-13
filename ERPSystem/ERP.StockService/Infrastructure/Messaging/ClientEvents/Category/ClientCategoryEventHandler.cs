// Infrastructure/Messaging/ClientEvents/Category/ClientCategoryEventHandler.cs
using ERP.StockService.Application.DTOs;
using ERP.StockService.Application.Interfaces;

namespace ERP.StockService.Infrastructure.Messaging.ClientEvents.Category;

public sealed class ClientCategoryEventHandler : IClientCategoryEventHandler
{
    private readonly IClientCategoryCacheService _clientCategoryCacheService;
    private readonly ILogger<ClientCategoryEventHandler> _logger;

    public ClientCategoryEventHandler(
        IClientCategoryCacheService clientCategoryCacheService,
        ILogger<ClientCategoryEventHandler> logger)
    {
        _clientCategoryCacheService = clientCategoryCacheService;
        _logger = logger;
    }

    public async Task HandleCreatedAsync(ClientCategoryResponseDto dto)
    {
        try
        {
            _logger.LogInformation("Handling client category creation: {CategoryName} ",
                dto.Name);

            await _clientCategoryCacheService.SyncCreatedAsync(dto);

            _logger.LogInformation("Successfully handled client category creation: {CategoryName}", dto.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling client category creation: {CategoryName}", dto.Name);
            throw;
        }
    }

    public async Task HandleUpdatedAsync(ClientCategoryResponseDto dto)
    {
        try
        {
            _logger.LogInformation("Handling client category update: {CategoryName} (Id: {CategoryId})",
                dto.Name, dto.Id);

            await _clientCategoryCacheService.SyncUpdatedAsync(dto);

            _logger.LogInformation("Successfully handled client category update: {CategoryName}", dto.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling client category update: {CategoryId}", dto.Id);
            throw;
        }
    }

    public async Task HandleDeletedAsync(ClientCategoryResponseDto dto)
    {
        try
        {
            _logger.LogInformation("Handling client category deletion: {CategoryId}", dto.Id);
            await _clientCategoryCacheService.SyncDeletedAsync(dto);

            _logger.LogInformation("Successfully handled client category deletion: {CategoryId}", dto.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling client category deletion: {CategoryId}", dto.Id);
            throw;
        }
    }

    public async Task HandleRestoredAsync(ClientCategoryResponseDto dto)
    {
        try
        {
            _logger.LogInformation("Handling client category restoration: {CategoryName} (Id: {CategoryId})",
                dto.Name, dto.Id);

            // For restore, we can either recreate or update the category
            await _clientCategoryCacheService.SyncRestoredAsync(dto);

            _logger.LogInformation("Successfully handled client category restoration: {CategoryName}", dto.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling client category restoration: {CategoryId}", dto.Id);
            throw;
        }
    }
}