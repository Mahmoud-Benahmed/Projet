namespace ERP.ArticleService.Application.DTOs
{
    public record ArticleStatsDto(
        int TotalCount,
        int ActiveCount,
        int InActiveCount,
        int CategoriesCount
    );
}
