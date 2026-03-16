namespace ERP.AuthService.Application.Exceptions
{
    public class PwnedPasswordException : Exception
    {
        public PwnedPasswordException(string msg): base(msg) {}
    }
}
