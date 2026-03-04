namespace ERP.ArticleService.Application.DTOs
{
    public record CreateArticleRequest(string Libelle, decimal Prix, Guid CategoryId);
    public record UpdateArticleRequest(string Libelle, decimal Prix, Guid CategoryId);
}
