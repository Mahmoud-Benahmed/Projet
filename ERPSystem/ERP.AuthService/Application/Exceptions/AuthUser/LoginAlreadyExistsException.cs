namespace ERP.AuthService.Application.Exceptions.AuthUser
{
    public class LoginAlreadyExsistException : Exception
    {
        public LoginAlreadyExsistException() : base("Login already exists.")
        {
        }
        public LoginAlreadyExsistException(string message) : base(message)
        {
        }
        public LoginAlreadyExsistException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
