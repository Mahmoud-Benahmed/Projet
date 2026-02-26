namespace ERP.UserService.Application.Exceptions
{
    public class InvalidUserProfileException : ArgumentException
    {
        public InvalidUserProfileException() : base("Invalid value") { }
        public InvalidUserProfileException(string message) : base(message) { }
    }
}
