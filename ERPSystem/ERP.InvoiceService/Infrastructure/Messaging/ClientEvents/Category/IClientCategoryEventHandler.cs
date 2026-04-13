using ERP.InvoiceService.Application.DTOs;

namespace ERP.InvoiceService.Application.Interfaces;

public interface IClientCategoryEventHandler
{
    Task HandleCreatedAsync(ClientCategoryResponseDto dto);
    Task HandleUpdatedAsync(ClientCategoryResponseDto dto);
    Task HandleDeletedAsync(ClientCategoryResponseDto dto);
    Task HandleRestoredAsync(ClientCategoryResponseDto dto);
}