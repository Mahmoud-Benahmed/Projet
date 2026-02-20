namespace ERP.UserService.Domain;
using ERP.UserService.Application.Exceptions;
public class UserProfile
{
    public Guid Id { get; private set; }
    public Guid AuthUserId { get; private set; }
    public string Email { get; private set; }

    public string? FullName { get; private set; }
    public string? Phone { get; private set; }

    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private UserProfile() { }

    public UserProfile(Guid authUserId, string email)
    {
        if (authUserId == Guid.Empty)
            throw new ArgumentException("AuthUserId cannot be empty.");

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.");

        Id = Guid.NewGuid();
        AuthUserId = authUserId;
        Email = email;
        CreatedAt = DateTime.UtcNow;
        IsActive = true;
    }

    public void CompleteProfile(string fullName, string phone)
    {
        if (!IsActive)
            throw new UserNotActiveException();

        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name is required.");

        if (string.IsNullOrWhiteSpace(phone))
            throw new ArgumentException("Phone is required.");

        FullName = fullName;
        Phone = phone;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsProfileCompleted() =>
        !string.IsNullOrWhiteSpace(FullName) &&
        !string.IsNullOrWhiteSpace(Phone);

    public void Activate()
    {
        if (IsActive) return;
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        if (!IsActive) return;
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}