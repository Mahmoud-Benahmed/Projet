using ERP.StockService.Application.DTOs;
using ERP.StockService.Application.Interfaces;
using ERP.StockService.Domain.LocalCache.Article;
using ERP.StockService.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ERP.StockService.Application.Services.LocalCache.ArticleCache;

public sealed class CategoryCacheService : ICategoryCacheService
{
    private readonly ICategoryCacheRepository _repo;
    private readonly ILogger<CategoryCacheService> _logger;

    public CategoryCacheService(
        ICategoryCacheRepository repo,
        ILogger<CategoryCacheService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    // ── Queries ───────────────────────────────────────────────────────────────

    public async Task<CategoryResponseDto?> GetByIdAsync(Guid id)
    {
        var category = await _repo.GetByIdAsync(id);
        return category is null ? null : MapToDto(category);
    }

    public async Task<CategoryResponseDto?> GetByNameAsync(string name)
    {
        var category = await _repo.GetByNameAsync(name);
        return category is null ? null : MapToDto(category);
    }


    public async Task<List<CategoryResponseDto>> GetAllAsync()
    {
        var categories = await _repo.GetAllAsync();
        return categories.Select(MapToDto).ToList();
    }

    public async Task<List<CategoryResponseDto>> GetAllActiveAsync()
    {
        var categories = await _repo.GetAllActiveAsync();
        return categories.Select(MapToDto).ToList();
    }

    public async Task<PagedResultDto<CategoryResponseDto>> GetPagedAsync(int pageNumber, int pageSize)
    {
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber));
        if (pageSize < 1) throw new ArgumentOutOfRangeException(nameof(pageSize));

        var (items, totalCount) = await _repo.GetPagedAsync(pageNumber, pageSize);
        return new PagedResultDto<CategoryResponseDto>(
            items.Select(MapToDto).ToList(),
            totalCount,
            pageNumber,
            pageSize);
    }

    public async Task<bool> ExistsAsync(string name)
    {
        return await _repo.ExistsAsync(name);
    }

    // ── Kafka sync ────────────────────────────────────────────────────────────

    // CategoryCacheService.cs
    public async Task SyncCreatedAsync(CategoryResponseDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            _logger.LogWarning("Category event has null or empty Name. Id: {CategoryId}", dto.Id);
            return;
        }

        try
        {
            // Try to find by ID first, then by Name
            var existing = await _repo.GetByIdAsync(dto.Id) ?? await _repo.GetByNameAsync(dto.Name);

            if (existing != null)
            {
                _logger.LogInformation(
                    existing.Id == dto.Id
                        ? "Category {Name} (Id: {Id}) found. Updating."
                        : "Category name '{Name}' found with different ID (Existing: {ExistingId}, New: {NewId}). Updating existing.",
                    dto.Name, dto.Id, existing.Id);

                existing.ApplyUpdate(dto);
                await _repo.SaveChangesAsync();
                return;
            }

            // Create new category
            _logger.LogInformation("Creating new category: {Name} (Id: {Id})", dto.Name, dto.Id);
            await _repo.AddAsync(CategoryCache.FromEvent(dto));
            await _repo.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("duplicate") == true)
        {
            // Race condition - another instance created it first
            _logger.LogWarning(ex, "Duplicate category detected for '{Name}'. Attempting to retrieve existing...", dto.Name);

            // Wait a bit and try to get the category that was just created
            await Task.Delay(100);

            var existing = await _repo.GetByNameAsync(dto.Name);
            if (existing != null)
            {
                _logger.LogInformation("Found existing category '{Name}'. Updating instead.", dto.Name);
                existing.ApplyUpdate(dto);
                await _repo.SaveChangesAsync();
            }
            else
            {
                _logger.LogError("Could not recover from duplicate error for category '{Name}'", dto.Name);
                throw;
            }
        }
    }
    public async Task SyncUpdatedAsync(CategoryResponseDto dto)
    {
        var existing = await _repo.GetByIdAsync(dto.Id);
        if (existing is null)
        {
            _logger.LogWarning("SyncUpdated: category {Id} not in cache, inserting instead", dto.Id);
            await _repo.AddAsync(CategoryCache.FromEvent(dto));
        }
        else
        {
            existing.ApplyUpdate(dto);
        }

        await _repo.SaveChangesAsync();
        _logger.LogInformation("ArticleCache synced (updated) for {Id} — {Libelle}", dto.Id, dto.Name);
    }

    public async Task SyncDeletedAsync(CategoryResponseDto dto)
    {
        var existing = await _repo.GetByIdAsync(dto.Id);
        if (existing is null)
        {
            _logger.LogWarning("SyncDeleted: category {Id} not in cache, skipping", dto.Id);
            return;
        }

        existing.MarkDeleted();
        await _repo.SaveChangesAsync();
        _logger.LogInformation("ArticleCache marked deleted for {Id}", dto.Id);
    }

    public async Task SyncRestoredAsync(CategoryResponseDto dto)
    {
        var existing = await _repo.GetByIdAsync(dto.Id);
        if (existing is null)
        {
            _logger.LogWarning("SyncRestored: article {Id} not in cache, inserting instead", dto.Id);
            await _repo.AddAsync(CategoryCache.FromEvent(dto));
        }
        else
        {
            existing.MarkRestored();
        }

        await _repo.SaveChangesAsync();
        _logger.LogInformation("ArticleCache marked restored for {Id}", dto.Id);
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static CategoryResponseDto MapToDto(CategoryCache c) => new(
        Id: c.Id,
        Name: c.Name,
        TVA: c.TVA,
        IsDeleted: c.IsDeleted,
        CreatedAt: c.CreatedAt,
        UpdatedAt: c.UpdatedAt);
}