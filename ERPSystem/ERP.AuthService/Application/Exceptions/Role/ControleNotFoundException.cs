namespace ERP.AuthService.Application.Exceptions.Role
{
    public class ControleNotFoundException : Exception
    {
        public ControleNotFoundException(Guid id) : base($"Controle with id '{id}' was not found.") { }
        public ControleNotFoundException(string message) : base(message) { }
    }
}
