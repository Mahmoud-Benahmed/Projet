using ERP.InvoiceService.Application.DTOs;

namespace ERP.InvoiceService.Domain.LocalCache.Article;

public sealed class CategoryCache
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public decimal TVA { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private CategoryCache() { }
    public static CategoryCache FromEvent(CategoryResponseDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        TVA = dto.TVA,
        IsDeleted = dto.IsDeleted,
        CreatedAt = dto.CreatedAt,
        UpdatedAt = dto.UpdatedAt,
    };

    public void ApplyUpdate(CategoryResponseDto dto)
    {
        Name = dto.Name;
        TVA = dto.TVA;
        IsDeleted = dto.IsDeleted;
        UpdatedAt = dto.UpdatedAt;
    }

    public void MarkDeleted() => IsDeleted = true;
    public void MarkRestored() => IsDeleted = false;

}