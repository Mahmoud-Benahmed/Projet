using ERP.StockService.Application.DTOs;

namespace ERP.StockService.Infrastructure.Messaging.Events.InvoiceEvents;


public interface IInvoiceEventHandler
{
    Task HandleCreatedAsync(InvoiceDto dto);
    Task HandleCancelledAsync(InvoiceDto dto);
}