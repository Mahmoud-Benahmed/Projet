namespace ERP.StockService.Application.Exceptions;
public class ClientNotFoundException : Exception
{
    public ClientNotFoundException(Guid id)
        : base($"Client with id '{id}' was not found.") { }
}
