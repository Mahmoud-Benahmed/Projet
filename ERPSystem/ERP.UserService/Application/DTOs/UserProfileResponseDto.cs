namespace ERP.UserService.Application.DTOs;

public class UserProfileResponseDto
{
    public Guid Id { get; set; }

    public required Guid AuthUserId { get; set; }
    public required string Login { get; set; } = default!;
    public required string Email { get; set; } = default!;

    public string? FullName { get; set; }

    public string? Phone { get; set; }

    public required string Role { get; set; }


    public bool IsActive { get; set; }

    public bool IsProfileCompleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}