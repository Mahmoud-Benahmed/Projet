namespace ERP.UserService.Domain;

using ERP.UserService.Application.Exceptions;
public class UserProfile
{
    public Guid Id { get; private set; }
    public Guid AuthUserId { get; private set; }
    public string Login { get; private set; }
    public string Email { get; private set; }

    public string? FullName { get; private set; }
    public string? Phone { get; private set; }

    public string Role { get; private set; }

    public bool IsActive { get; private set; } = false;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private UserProfile() { }

    public UserProfile(string login, string role, Guid authUserId, string email)
    {
        if (string.IsNullOrEmpty(login))
            throw new ArgumentNullException("Login canot be empty");

        if (string.IsNullOrEmpty(login))
            throw new ArgumentNullException("Role canot be empty");

        if (authUserId == Guid.Empty)
            throw new ArgumentException("AuthUserId cannot be empty.");

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.");

        Id = Guid.NewGuid();
        Login = login;
        Role = role;
        AuthUserId = authUserId;
        Email = email;
        CreatedAt = DateTime.UtcNow;
    }

    public void CompleteProfile(string fullName, string phone)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name is required.");

        if (string.IsNullOrWhiteSpace(phone))
            throw new ArgumentException("Phone is required.");

        if (IsProfileCompleted() && !IsActive)
            throw new InvalidOperationException(
                "Cannot complete profile: profile already completed but account is not active."
            );

        FullName = fullName;
        Phone = phone;
        UpdatedAt = DateTime.UtcNow;

        // Activate if not active
        if (!IsActive)
            Activate();
    }

    public bool IsProfileCompleted() =>
        !string.IsNullOrWhiteSpace(FullName) &&
        !string.IsNullOrWhiteSpace(Phone);

    public void Activate()
    {
        if (IsActive)
            return;
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        if (!IsActive)
            throw new UserNotActiveException("User already NOT active");
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

}