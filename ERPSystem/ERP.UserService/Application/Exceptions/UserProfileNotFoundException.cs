namespace ERP.UserService.Application.Exceptions;

public class UserProfileNotFoundException : Exception
{
    public UserProfileNotFoundException(Guid id)
        : base($"User profile with Id '{id}' was not found.")
    {
    }

    public UserProfileNotFoundException(string message)
        : base(message)
    {
    }
}