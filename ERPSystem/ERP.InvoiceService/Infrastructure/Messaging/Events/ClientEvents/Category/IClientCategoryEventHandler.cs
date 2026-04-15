using ERP.InvoiceService.Application.DTOs;

namespace ERP.InvoiceService.Infrastructure.Messaging.Events.ClientEvents.Category;

public interface IClientCategoryEventHandler
{
    Task HandleCreatedAsync(ClientCategoryResponseDto dto);
    Task HandleUpdatedAsync(ClientCategoryResponseDto dto);
    Task HandleDeletedAsync(ClientCategoryResponseDto dto);
    Task HandleRestoredAsync(ClientCategoryResponseDto dto);
}