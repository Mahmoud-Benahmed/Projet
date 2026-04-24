using ERP.InvoiceService.Application.DTOs;
using InvoiceService.Application.DTOs;

namespace ERP.InvoiceService.Application.Interfaces;

public interface IArticleCacheService
{
    Task<ArticleResponseDto?> GetByIdAsync(Guid id);
    Task<ArticleResponseDto?> GetByBarCodeAsync(string barCode);
    Task<ArticleResponseDto?> GetByCodeRefAsync(string codeRef);
    Task<List<ArticleResponseDto>> GetAllAsync();
    Task<List<ArticleResponseDto>> GetAllActiveAsync();
    Task<PagedResultDto<ArticleResponseDto>> GetPagedAsync(int pageNumber, int pageSize, string? search = null);

    // Called by the Kafka event handler
    Task SyncCreatedAsync(ArticleResponseDto dto);
    Task SyncUpdatedAsync(ArticleResponseDto dto);
    Task SyncDeletedAsync(ArticleResponseDto dto);
    Task SyncRestoredAsync(ArticleResponseDto dto);
}

public interface IArticleCategoryCacheService
{
    Task<bool> ExistsAsync(string name);
    Task<ArticleCategoryResponseDto?> GetByIdAsync(Guid id);
    Task<ArticleCategoryResponseDto?> GetByNameAsync(string name);
    Task<List<ArticleCategoryResponseDto>> GetAllAsync();
    Task<List<ArticleCategoryResponseDto>> GetAllActiveAsync();
    Task<PagedResultDto<ArticleCategoryResponseDto>> GetPagedAsync(int pageNumber, int pageSize);

    Task SyncCreatedAsync(ArticleCategoryResponseDto dto);
    Task SyncUpdatedAsync(ArticleCategoryResponseDto dto);
    Task SyncDeletedAsync(ArticleCategoryResponseDto dto);
    Task SyncRestoredAsync(ArticleCategoryResponseDto dto);
}

public interface IClientCacheService
{
    Task<ClientResponseDto?> GetByIdAsync(Guid id);
    Task<PagedResultDto<ClientResponseDto>> GetPagedAsync(int pageNumber, int pageSize, string? search = null);
    Task<bool> ExistsAsync(Guid id);
    Task SyncCreatedAsync(ClientResponseDto dto);
    Task SyncUpdatedAsync(ClientResponseDto dto);
    Task SyncDeletedAsync(ClientResponseDto dto);
    Task SyncRestoredAsync(ClientResponseDto dto);
}

public interface IClientCategoryCacheService
{
    // Read operations
    Task<ClientCategoryResponseDto?> GetByIdAsync(Guid id);
    Task<List<ClientCategoryResponseDto>> GetByClientIdAsync(Guid clientId);
    Task<List<ClientCategoryResponseDto>> GetAllAsync();
    Task<bool> ExistsAsync(Guid id);
    Task<bool> ExistsForClientAsync(Guid clientId, Guid categoryName);
    Task<int> GetCountForClientAsync(Guid clientId);

    // Sync operations (for Kafka events)
    Task SyncCreatedAsync(ClientCategoryResponseDto dto);
    Task SyncRangeCreatedAsync(List<ClientCategoryResponseDto> dtos, Guid clientId);
    Task SyncUpdatedAsync(ClientCategoryResponseDto dto);
    Task SyncRestoredAsync(ClientCategoryResponseDto dto);
    Task SyncDeletedAsync(ClientCategoryResponseDto dto);
    Task SyncDeletedForClientAsync(Guid clientId);
}