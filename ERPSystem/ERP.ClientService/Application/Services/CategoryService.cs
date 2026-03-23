using ERP.ClientService.Application.DTOs;
using ERP.ClientService.Application.Exceptions;
using ERP.ClientService.Application.Interfaces;
using ERP.ClientService.Domain;

namespace ERP.ClientService.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoryService(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    // =========================
    // CREATE
    // =========================
    public async Task<CategoryResponseDto> CreateAsync(CreateCategoryRequestDto request)
    {
        var existing = await _categoryRepository.GetByCodeAsync(request.Code);
        if (existing is not null)
            throw new CategoryAlreadyExistsException(request.Code);

        var category = Category.Create(
            request.Name, request.Code, request.DelaiRetour,
            request.UseBulkPricing, request.DiscountRate, request.CreditLimitMultiplier);

        await _categoryRepository.AddAsync(category);
        await _categoryRepository.SaveChangesAsync();
        return MapToDto(category);
    }

    // =========================
    // READ
    // =========================
    public async Task<CategoryResponseDto> GetByIdAsync(Guid id)
    {
        var category = await _categoryRepository.GetByIdAsync(id);
        if (category is null || category.IsDeleted)
            throw new CategoryNotFoundException(id);
        return MapToDto(category);
    }

    // =========================
    // UPDATE
    // =========================
    public async Task<CategoryResponseDto> UpdateAsync(Guid id, UpdateCategoryRequestDto request)
    {
        var category = await _categoryRepository.GetByIdAsync(id);
        if (category is null || category.IsDeleted)
            throw new CategoryNotFoundException(id);

        var normalised = request.Code.Trim().ToUpperInvariant();
        if (category.Code != normalised)
        {
            var existing = await _categoryRepository.GetByCodeAsync(request.Code);
            if (existing is not null)
                throw new CategoryAlreadyExistsException(request.Code);
        }

        category.Update(
            request.Name, request.Code, request.DelaiRetour,
            request.UseBulkPricing, request.DiscountRate, request.CreditLimitMultiplier);

        await _categoryRepository.SaveChangesAsync();
        return MapToDto(category);
    }

    // =========================
    // DELETE
    // =========================
    public async Task DeleteAsync(Guid id)
    {
        var category = await _categoryRepository.GetByIdAsync(id);
        if (category is null || category.IsDeleted)
            throw new CategoryNotFoundException(id);

        category.Delete();
        await _categoryRepository.SaveChangesAsync();
    }

    // =========================
    // RESTORE
    // =========================
    public async Task RestoreAsync(Guid id)
    {
        var category = await _categoryRepository.GetByIdDeletedAsync(id)
            ?? throw new CategoryNotFoundException(id);

        if (!category.IsDeleted)
            return;

        category.Restore();
        await _categoryRepository.SaveChangesAsync();
    }

    // =========================
    // ACTIVATE / DEACTIVATE
    // =========================
    public async Task<CategoryResponseDto> ActivateAsync(Guid id)
    {
        var category = await _categoryRepository.GetByIdAsync(id);
        if (category is null || category.IsDeleted)
            throw new CategoryNotFoundException(id);

        category.Activate();
        await _categoryRepository.SaveChangesAsync();
        return MapToDto(category);
    }

    public async Task<CategoryResponseDto> DeactivateAsync(Guid id)
    {
        var category = await _categoryRepository.GetByIdAsync(id);
        if (category is null || category.IsDeleted)
            throw new CategoryNotFoundException(id);

        category.Deactivate();
        await _categoryRepository.SaveChangesAsync();
        return MapToDto(category);
    }

    // =========================
    // PAGING / FILTERING
    // =========================
    public async Task<PagedResultDto<CategoryResponseDto>> GetAllAsync(
        int pageNumber, int pageSize)
    {
        ValidatePaging(pageNumber, pageSize);
        var (items, totalCount) = await _categoryRepository.GetAllAsync(pageNumber, pageSize);
        return new PagedResultDto<CategoryResponseDto>(items.Select(c => MapToDto(c)).ToList(), totalCount, pageNumber, pageSize);
    }

    public async Task<PagedResultDto<CategoryResponseDto>> GetPagedDeletedAsync(
        int pageNumber, int pageSize)
    {
        ValidatePaging(pageNumber, pageSize);
        var (items, totalCount) = await _categoryRepository
            .GetPagedDeletedAsync(pageNumber, pageSize);
        return new PagedResultDto<CategoryResponseDto>(
            items.Select(c => MapToDto(c)).ToList(), totalCount, pageNumber, pageSize);
    }

    public async Task<PagedResultDto<CategoryResponseDto>> GetPagedByNameAsync(string nameFilter, int pageNumber, int pageSize)
    {
        ValidatePaging(pageNumber, pageSize);
        if (string.IsNullOrWhiteSpace(nameFilter))
            throw new ArgumentException("Name filter cannot be empty.");

        var (items, totalCount) = await _categoryRepository
            .GetPagedByNameAsync(nameFilter, pageNumber, pageSize);
        return new PagedResultDto<CategoryResponseDto>(
            items.Select(c => MapToDto(c)).ToList(), totalCount, pageNumber, pageSize);
    }

    // =========================
    // STATS
    // =========================
    public async Task<CategoryStatsDto> GetStatsAsync() =>
        await _categoryRepository.GetStatsAsync();

    // =========================
    // PRIVATE HELPERS
    // =========================
    private static void ValidatePaging(int pageNumber, int pageSize)
    {
        if (pageNumber < 1)
            throw new ArgumentOutOfRangeException(nameof(pageNumber),
                "Page number must be greater than zero.");
        if (pageSize < 1)
            throw new ArgumentOutOfRangeException(nameof(pageSize),
                "Page size must be greater than zero.");
    }

    public CategoryResponseDto MapToDto(Category category)
    {
        return new CategoryResponseDto(
            Id: category.Id,
            Name: category.Name,
            Code: category.Code,
            DelaiRetour: category.DelaiRetour,
            DiscountRate: category.DiscountRate,
            CreditLimitMultiplier: category.CreditLimitMultiplier,
            UseBulkPricing: category.UseBulkPricing,
            IsActive: category.IsActive,
            IsDeleted: category.IsDeleted,
            CreatedAt: category.CreatedAt,
            UpdatedAt: category.UpdatedAt,
            ClientCount: category.ClientCategories.Count
        );
    }
}