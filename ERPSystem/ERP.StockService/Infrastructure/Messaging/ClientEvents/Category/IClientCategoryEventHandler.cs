using ERP.StockService.Application.DTOs;

namespace ERP.StockService.Application.Interfaces;

public interface IClientCategoryEventHandler
{
    Task HandleCreatedAsync(ClientCategoryResponseDto dto);
    Task HandleUpdatedAsync(ClientCategoryResponseDto dto);
    Task HandleDeletedAsync(ClientCategoryResponseDto dto);
    Task HandleRestoredAsync(ClientCategoryResponseDto dto);
}