namespace ERP.AuthService.Application.Exceptions.Role
{
    public class RoleNotFoundException : Exception
    {
        public RoleNotFoundException(Guid id) : base($"Role with id '{id}' was not found.") { }
        public RoleNotFoundException(string message) : base(message) { }
    }
}
