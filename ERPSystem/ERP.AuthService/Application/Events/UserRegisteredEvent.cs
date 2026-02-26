namespace ERP.AuthService.Application.Events
{
    public record UserRegisteredEvent(
        string AuthUserId,
        string Email
    );
}
