namespace ERP.InvoiceService.Application.DTOs
{
    public sealed record ClientResponseDto(
        Guid Id,
        string Name,
        string Email,
        string Address,
        string? Phone,
        string? TaxNumber,
        decimal? CreditLimit,
        int? DelaiRetour,
        bool IsBlocked,
        bool IsDeleted,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        List<ClientCategoryResponseDto> Categories
    );

    public sealed record ClientCategoryResponseDto(
        Guid Id,
        string Name,
        string Code,
        DateTime AssignedAt
    );


    public record CategoryResponseDto(
        Guid Id,
        string Name,
        decimal TVA,
        bool IsDeleted,
        DateTime CreatedAt,
        DateTime? UpdatedAt
    );
    public record ArticleResponseDto(
        Guid Id,
        CategoryResponseDto Category,
        string CodeRef,
        string BarCode,
        string Libelle,
        decimal Prix,
        decimal TVA,
        bool IsDeleted,
        DateTime CreatedAt,
        DateTime? UpdatedAt
        );

    public sealed class PagedResultDto<T>
    {
        public List<T> Items { get; }
        public int TotalCount { get; }
        public int PageNumber { get; }
        public int PageSize { get; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

        public PagedResultDto(List<T> items, int totalCount, int pageNumber, int pageSize)
        {
            Items = items;
            TotalCount = totalCount;
            PageNumber = pageNumber;
            PageSize = pageSize;
        }
    }
}
