namespace ERP.AuthService.Application.Exceptions.AuthUser
{
    public class UserInactiveException : Exception
    {
        public UserInactiveException() : base("User is inactive.")
        {
        }
        public UserInactiveException(string message) : base(message)
        {
        }
        public UserInactiveException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
