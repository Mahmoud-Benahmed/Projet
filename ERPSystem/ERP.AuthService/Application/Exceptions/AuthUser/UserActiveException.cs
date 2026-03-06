namespace ERP.AuthService.Application.Exceptions.AuthUser
{
    public class UserActiveException : UnauthorizedAccessException
    {
        public UserActiveException() : base("User already active")
        {
        }
        public UserActiveException(string message) : base(message)
        {
        }
    }
}