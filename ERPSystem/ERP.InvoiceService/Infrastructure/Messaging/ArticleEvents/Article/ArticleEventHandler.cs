using ERP.InvoiceService.Application.DTOs;
using ERP.InvoiceService.Application.Interfaces;

namespace ERP.InvoiceService.Infrastructure.Messaging.ArticleEvents.Article;

public sealed class ArticleEventHandler : IArticleEventHandler
{
    private readonly IArticleCacheService _cacheService;
    private readonly ILogger<ArticleEventHandler> _logger;

    public ArticleEventHandler(
        IArticleCacheService cacheService,
        ILogger<ArticleEventHandler> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public Task HandleCreatedAsync(ArticleResponseDto dto)
        => _cacheService.SyncCreatedAsync(dto);

    public Task HandleUpdatedAsync(ArticleResponseDto dto)
        => _cacheService.SyncUpdatedAsync(dto);

    public Task HandleDeletedAsync(ArticleResponseDto dto)
        => _cacheService.SyncDeletedAsync(dto);

    public Task HandleRestoredAsync(ArticleResponseDto dto)
        => _cacheService.SyncRestoredAsync(dto);
}