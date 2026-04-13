using ERP.InvoiceService.Application.DTOs;
using ERP.InvoiceService.Application.Interfaces;

namespace ERP.InvoiceService.Infrastructure.Messaging.ArticleEvents.Category;

public sealed class CategoryEventHandler : ICategoryEventHandler
{
    private readonly ICategoryCacheService _cacheService;
    private readonly ILogger<CategoryEventHandler> _logger;

    public CategoryEventHandler(
        ICategoryCacheService cacheService,
        ILogger<CategoryEventHandler> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public Task HandleCreatedAsync(CategoryResponseDto dto)
        => _cacheService.SyncCreatedAsync(dto);

    public Task HandleUpdatedAsync(CategoryResponseDto dto)
        => _cacheService.SyncUpdatedAsync(dto);

    public Task HandleDeletedAsync(CategoryResponseDto dto)
        => _cacheService.SyncDeletedAsync(dto);

    public Task HandleRestoredAsync(CategoryResponseDto dto)
        => _cacheService.SyncRestoredAsync(dto);
}