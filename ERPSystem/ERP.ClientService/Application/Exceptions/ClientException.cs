namespace ERP.ClientService.Application.Exceptions;

public sealed class ClientNotFoundException : KeyNotFoundException
{
    public ClientNotFoundException(Guid id)
        : base($"Client with id '{id}' was not found.") { }

    public ClientNotFoundException(string email)
        : base($"Client with email '{email}' was not found.") { }
}

public sealed class ClientAlreadyExistsException : InvalidOperationException
{
    public ClientAlreadyExistsException(string email)
        : base($"A client with email '{email}' already exists.") { }
}

public sealed class ClientBlockedException : InvalidOperationException
{
    public ClientBlockedException(Guid id)
        : base($"Client '{id}' is blocked and cannot perform this operation.") { }
}