namespace ERP.UserService.Application.DTOs
{
    public record CreateUserProfileDto(
        string Login,
        string Role,
        Guid AuthUserId,
        string Email
    );
}
