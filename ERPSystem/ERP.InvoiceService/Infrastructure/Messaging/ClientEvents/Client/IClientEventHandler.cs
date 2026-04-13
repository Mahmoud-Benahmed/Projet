using ERP.InvoiceService.Application.DTOs;

namespace ERP.InvoiceService.Infrastructure.Messaging.ClientEvents.Client;

public interface IClientEventHandler
{
    Task HandleCreatedAsync(ClientResponseDto dto);
    Task HandleUpdatedAsync(ClientResponseDto dto);
    Task HandleDeletedAsync(ClientResponseDto dto);
    Task HandleRestoredAsync(ClientResponseDto dto);
}