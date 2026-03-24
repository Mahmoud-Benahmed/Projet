namespace ERP.AuthService.Application.Exceptions.Role
{
    public class ControleNotFoundException : Exception
    {
        public ControleNotFoundException(Guid id) : base($"Controle with id '{id}' was not found.") { }
        public ControleNotFoundException(string message) : base(message) { }
    }
    public class ControleAlreadyExistException : Exception
    {
        public ControleAlreadyExistException(Guid id) : base($"A duplicate Controle with id '{id}' was found.") { }
        public ControleAlreadyExistException(string message) : base(message) { }
    }
}
