namespace ERP.UserService.Application.Exceptions;

public class UserProfileAlreadyExistsException : Exception
{
    public UserProfileAlreadyExistsException(Guid authUserId)
        : base($"User profile for AuthUserId '{authUserId}' already exists.")
    {
    }
}