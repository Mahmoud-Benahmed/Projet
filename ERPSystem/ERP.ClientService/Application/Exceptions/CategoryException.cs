namespace ERP.ClientService.Application.Exceptions
{
    public sealed class CategoryNotFoundException : KeyNotFoundException
    {
        public CategoryNotFoundException(Guid id)
            : base($"Category with id '{id}' was not found.") { }

        public CategoryNotFoundException(string code)
            : base($"Category with code '{code}' was not found.") { }
    }

    public sealed class CategoryAlreadyExistsException : InvalidOperationException
    {
        public CategoryAlreadyExistsException(string code)
            : base($"A category with code '{code}' already exists.") { }
    }
}
