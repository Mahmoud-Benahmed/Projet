namespace ERP.UserService.Application.Exceptions
{
    public class UnauthorizedOperationException: Exception
    {
        public UnauthorizedOperationException(string message) : base(message) { }
    }
}
