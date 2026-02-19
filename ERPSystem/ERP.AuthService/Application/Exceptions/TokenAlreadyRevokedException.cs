namespace ERP.AuthService.Application.Exceptions
{
    public class TokenAlreadyRevokedException: Exception
    {
        public TokenAlreadyRevokedException() : base("Token already revoked.")
        {
        }
        public TokenAlreadyRevokedException(string message) : base(message)
        {
        }
        public TokenAlreadyRevokedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
