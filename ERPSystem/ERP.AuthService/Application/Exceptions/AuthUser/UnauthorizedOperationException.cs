namespace ERP.AuthService.Application.Exceptions.AuthUser
{
    public class UnauthorizedOperationException : Exception
    {
        public UnauthorizedOperationException(string message) : base(message) { }
    }
}
