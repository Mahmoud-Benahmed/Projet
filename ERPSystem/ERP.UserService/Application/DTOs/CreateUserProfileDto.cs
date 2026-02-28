namespace ERP.UserService.Application.DTOs
{
    public record CreateUserProfileDto(
        string Login,
        Guid AuthUserId,
        string Email
    );
}
