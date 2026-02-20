namespace ERP.UserService.Application.DTOs
{
    public class CreateUserProfileDto
    {
        public Guid AuthUserId { get; set; }

        public string Email { get; set; } = default!;
    }
}
