namespace ERP.UserService.Application.Exceptions
{
    public class UserNotActiveException: UnauthorizedAccessException
    {
        public UserNotActiveException() : base("User NOT ACTIVE, operation NOT ALLOWED.")
        {
        }
        public UserNotActiveException(string message) : base(message)
        {
        }
    }
}
