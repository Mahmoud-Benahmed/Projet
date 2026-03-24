namespace ERP.AuthService.Application.Exceptions.Role
{
    public class RoleNotFoundException : Exception
    {
        public RoleNotFoundException(Guid id) : base($"Role with id '{id}' was not found.") { }
        public RoleNotFoundException(string message) : base(message) { }
    }

    public class RoleAlreadyExistException : Exception
    {
        public RoleAlreadyExistException(Guid id) : base($"A duplicate role with id '{id}' was found.") { }
        public RoleAlreadyExistException(string libelle) : base($"A duplicate role with Libelle '{libelle}' was found.") { }
    }
}
