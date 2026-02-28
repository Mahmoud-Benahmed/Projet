namespace ERP.UserService.Application.Events
{
    public record UserRegistered(
        string Login,
        string Role,
        string AuthUserId,
        string Email
    );
}