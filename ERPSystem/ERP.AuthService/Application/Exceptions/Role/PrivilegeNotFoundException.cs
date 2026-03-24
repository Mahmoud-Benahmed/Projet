namespace ERP.AuthService.Application.Exceptions.Role
{
    public class PrivilegeNotFoundException : Exception
    {
        public PrivilegeNotFoundException(Guid roleId, Guid controleId) : base($"Privilege  with RoleId '{roleId}' & ControleId '{controleId}' was not found.") { }
        public PrivilegeNotFoundException(Guid id) : base($"Privilege with id '{id}' was not found.") { }
        public PrivilegeNotFoundException(string message) : base(message) { }
    }

    public class PrivilegeAlreadyExistException : Exception
    {
        public PrivilegeAlreadyExistException(Guid roleId, Guid controleId) : base($"A duplicate Privilege with RoleId '{roleId}' & ControleId '{controleId}' was found.") { }
        public PrivilegeAlreadyExistException(Guid id) : base($"A duplicate Privilege with id '{id}' was found.") { }
        public PrivilegeAlreadyExistException(string message) : base(message) { }
    }
}
