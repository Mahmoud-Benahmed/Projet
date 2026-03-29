namespace ERP.StockService.Application.Exceptions;
public class ArticleNotFoundException : Exception
{
    public ArticleNotFoundException(Guid id)
        : base($"Article with id '{id}' was not found.") { }

    public ArticleNotFoundException(string code)
        : base($"Article with code '{code}' was not found.") { }
}
