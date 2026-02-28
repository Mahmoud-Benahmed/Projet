namespace ERP.UserService.Application.Events
{
    public record UserRegisteredEvent(
        string Login,
        string AuthUserId,
        string Email
    );
}