// ── Article Exceptions

namespace ERP.ArticleService.Application.Exceptions
{
    public class ArticleNotFoundException : Exception
    {
        public ArticleNotFoundException(Guid id)
            : base($"Article with id '{id}' was not found.") { }

        public ArticleNotFoundException(string code)
            : base($"Article with code '{code}' was not found.") { }
    }

    public class ArticleAlreadyExistsException : Exception
    {
        public ArticleAlreadyExistsException(string code)
            : base($"An article with code '{code}' already exists.") { }
    }

    public class ArticleAlreadyActiveException : Exception
    {
        public ArticleAlreadyActiveException(Guid id)
            : base($"Article with id '{id}' is already recovered.") { }
    }

    public class ArticleAlreadyInactiveException : Exception
    {
        public ArticleAlreadyInactiveException(Guid id)
            : base($"Article with id '{id}' is already inactive.") { }
    }
}