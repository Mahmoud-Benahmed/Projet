namespace ERP.AuthService.Application.Exceptions.Role
{
    public class PrivilegeNotFoundException : Exception
    {
        public PrivilegeNotFoundException(Guid roleId, Guid controleId) : base($"Privilege  with RoleId '{roleId}' & ControleId '{controleId}' was not found.") { }
        public PrivilegeNotFoundException(Guid id) : base($"Role with id '{id}' was not found.") { }
        public PrivilegeNotFoundException(string message) : base(message) { }
    }
}
