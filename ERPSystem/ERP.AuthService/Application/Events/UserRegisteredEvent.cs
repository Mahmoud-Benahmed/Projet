namespace ERP.AuthService.Application.Events
{
    public record UserRegisteredEvent(
        string Login,
        string AuthUserId,
        string Email
    );
}
