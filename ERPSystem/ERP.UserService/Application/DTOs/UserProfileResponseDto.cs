namespace ERP.UserService.Application.DTOs;

public class UserProfileResponseDto
{
    public Guid Id { get; set; }

    public Guid AuthUserId { get; set; }
    public string Login { get; set; } = default!;
    public string Email { get; set; } = default!;

    public string? FullName { get; set; }

    public string? Phone { get; set; }

    public bool IsActive { get; set; }

    public bool IsProfileCompleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}