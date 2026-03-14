namespace ERP.ClientService.Application.Exceptions
{
    public class ClientNotFoundException : Exception
    {
        public ClientNotFoundException(Guid id)
            : base($"Client with id '{id}' was not found.") { }

        public ClientNotFoundException(string email)
            : base($"Client with email '{email}' was not found.") { }
    }

    public class ClientAlreadyExistsException : Exception
    {
        public ClientAlreadyExistsException(string email)
            : base($"A Client with email '{email}' already exists.") { }
    }

    public class ClientAlreadyRestoredException : Exception
    {
        public ClientAlreadyRestoredException(Guid id)
            : base($"Client with id '{id}' already restored.") { }
    }

    public class ClientAlreadyDeletedException : Exception
    {
        public ClientAlreadyDeletedException(Guid id)
            : base($"Client with id '{id}' already deleted.") { }
    }
}