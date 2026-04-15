using ERP.StockService.Application.DTOs;

namespace ERP.StockService.Infrastructure.Messaging.Events.FournisseurEvents;

public interface IFournisseurEventHandler
{
    Task HandleCreatedAsync(FournisseurResponseDto dto);
    Task HandleUpdatedAsync(FournisseurResponseDto dto);
    Task HandleDeletedAsync(FournisseurResponseDto dto);
    Task HandleRestoredAsync(FournisseurResponseDto dto);
}