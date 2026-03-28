// ── Category Exceptions

namespace ERP.ArticleService.Application.Exceptions
{
    public class CategoryNotFoundException : Exception
    {
        public CategoryNotFoundException(Guid id)
            : base($"Category with id '{id}' was not found.") { }

        public CategoryNotFoundException(string name)
            : base($"Category with name '{name}' was not found.") { }
    }

    public class CategoryAlreadyExistsException : Exception
    {
        public CategoryAlreadyExistsException(string name)
            : base($"A category with the name '{name}' already exists.") { }
    }

    public class CategoryAssignedToArticlesException : InvalidOperationException
    {
        public CategoryAssignedToArticlesException()
            : base($"This catgeory is assigned to existing clients.") { }

        public CategoryAssignedToArticlesException(string message)
            : base(message) { }
    }


}