namespace ERP.AuthService.Application.Exceptions.AuthUser
{
    public class UserNotFoundException : Exception
    {
        public UserNotFoundException(string login)
            : base($"User with login '{login}' was not found.")
        {
        }

        public UserNotFoundException(Guid id)
            : base($"User with id '{id}' was not found.") { }
    }
}
