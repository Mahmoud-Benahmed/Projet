using ERP.StockService.Application.DTOs;
using ERP.StockService.Application.Interfaces;

namespace ERP.StockService.Infrastructure.Messaging.Events.InvoiceEvents;

public sealed class InvoiceEventHandler : IInvoiceEventHandler
{
    private readonly IInvoiceCacheService _cacheService;
    private readonly ILogger<InvoiceEventHandler> _logger;

    public InvoiceEventHandler(
        IInvoiceCacheService cacheService,
        ILogger<InvoiceEventHandler> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public Task HandleCreatedAsync(InvoiceDto dto)
        => _cacheService.SyncCreatedAsync(dto);

    public Task HandleCancelledAsync(InvoiceDto dto)
        => _cacheService.SyncCancelledAsync(dto);
}