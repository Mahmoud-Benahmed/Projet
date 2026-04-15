using ERP.StockService.Application.DTOs;

namespace ERP.StockService.Infrastructure.Messaging.Events.ClientEvents.Category;

public interface IClientCategoryEventHandler
{
    Task HandleCreatedAsync(ClientCategoryResponseDto dto);
    Task HandleUpdatedAsync(ClientCategoryResponseDto dto);
    Task HandleDeletedAsync(ClientCategoryResponseDto dto);
    Task HandleRestoredAsync(ClientCategoryResponseDto dto);
}