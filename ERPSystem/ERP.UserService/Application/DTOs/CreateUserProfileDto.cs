namespace ERP.UserService.Application.DTOs
{
    public record CreateUserProfileDto(
        Guid AuthUserId,
        string Email
    );
}
