using ERP.InvoiceService.Application.DTOs;
using ERP.InvoiceService.Application.Interfaces;

namespace ERP.InvoiceService.Infrastructure.Messaging.Events.ArticleEvents.ArticleCategory;

public sealed class ArticleCategoryEventHandler : IArticleCategoryEventHandler
{
    private readonly IArticleCategoryCacheService _cacheService;
    private readonly ILogger<ArticleCategoryEventHandler> _logger;

    public ArticleCategoryEventHandler(
        IArticleCategoryCacheService cacheService,
        ILogger<ArticleCategoryEventHandler> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public Task HandleCreatedAsync(ArticleCategoryResponseDto dto)
        => _cacheService.SyncCreatedAsync(dto);

    public Task HandleUpdatedAsync(ArticleCategoryResponseDto dto)
        => _cacheService.SyncUpdatedAsync(dto);

    public Task HandleDeletedAsync(ArticleCategoryResponseDto dto)
        => _cacheService.SyncDeletedAsync(dto);

    public Task HandleRestoredAsync(ArticleCategoryResponseDto dto)
        => _cacheService.SyncRestoredAsync(dto);
}