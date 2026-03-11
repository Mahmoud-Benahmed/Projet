namespace ERP.ArticleService.Application.DTOs
{
    public record ArticleStatsDto(
        int TotalCount,
        int ActiveCount,
        int DeletedCount,
        int CategoriesCount
    );
}
