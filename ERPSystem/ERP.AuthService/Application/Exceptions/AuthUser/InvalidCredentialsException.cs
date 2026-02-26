namespace ERP.AuthService.Application.Exceptions.AuthUser
{
    public class InvalidCredentialsException : Exception
    {
        public InvalidCredentialsException() : base("Invalid credentials.")
        {
        }
        public InvalidCredentialsException(string message) : base(message)
        {
        }
        public InvalidCredentialsException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
