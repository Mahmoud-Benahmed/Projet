namespace ERP.ClientService.Application.Exceptions
{
    public class CategoryNotFoundException : KeyNotFoundException
    {
        public CategoryNotFoundException(Guid id)
            : base($"Category with id '{id}' was not found.") { }

        public CategoryNotFoundException(string code)
            : base($"Category with code '{code}' was not found.") { }
    }

    public class CategoryAlreadyExistsException : InvalidOperationException
    {
        public CategoryAlreadyExistsException(string code)
            : base($"A category with code '{code}' already exists.") { }
    }

    public class CategoryAssignedToUsersException : InvalidOperationException
    {
        public CategoryAssignedToUsersException()
            : base($"This catgeory is assigned to existing clients.") { }

        public CategoryAssignedToUsersException(string message)
            : base(message) { }
    }
}
