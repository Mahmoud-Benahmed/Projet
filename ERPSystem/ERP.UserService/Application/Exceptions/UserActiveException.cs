namespace ERP.UserService.Application.Exceptions
{
    public class UserActiveException : UnauthorizedAccessException
    {
        public UserActiveException() : base("User is ACTIVE, operation NOT ALLOWED.")
        {
        }
        public UserActiveException(string message) : base(message)
        {
        }
    }
}
