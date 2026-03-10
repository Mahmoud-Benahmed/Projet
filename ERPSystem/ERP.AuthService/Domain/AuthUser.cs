using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;


namespace ERP.AuthService.Domain
{
    public class AuthUser
    {
        [BsonId]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid Id { get; private set; }

        [BsonRequired]
        public string Login { get; private set; }

        public string Email { get; set; } = default!;

        public string FullName { get; set; }

        public string PasswordHash { get; set; } = default!;

        public bool MustChangePassword { get; set; } = true;

        public bool IsActive { get; private set; } = true;

        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid RoleId { get; private set; }

        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        public DateTime? LastLoginAt { get; private set; }



        private AuthUser() { }

        public AuthUser(string login, string email, string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                throw new ArgumentNullException("FullName is required");

            if (string.IsNullOrWhiteSpace(login))
                throw new ArgumentException("Username is required");

            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email is required");

            Id = Guid.NewGuid();
            Email = email;
            Login = login;
            FullName = fullName;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;

        }

        public void UpdateProfile(string fullname, string email)
        {
            if (string.IsNullOrWhiteSpace(fullname))
                throw new ArgumentException("FullName is required");

            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email is required");

            FullName = fullname;
            Email = email;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetPasswordHash(string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(passwordHash))
                throw new ArgumentException("Password hash is required");
            PasswordHash = passwordHash;
        }
        public void SetRole(Guid roleId)
        {
            RoleId = roleId;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Deactivate()
        {
            if(!IsActive) return;
            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Activate()
        {
            if(IsActive) return;
            IsActive = true;
            UpdatedAt = DateTime.UtcNow;
        }

        public void ForcePasswordChange()
        {
            MustChangePassword = true;
        }

        public bool HasLoggedInBefore() => LastLoginAt != null;

        public bool CanLogin()
        {
            if (!IsActive && HasLoggedInBefore())
                return false;
            return true;
        }

        public void ChangePassword(string newPasswordHash)
        {
            if (string.IsNullOrWhiteSpace(newPasswordHash))
                throw new ArgumentException("Password hash is required");

            PasswordHash = newPasswordHash;

            UpdatedAt = DateTime.UtcNow;
        }

        public void RecordLogin()
        {
            if (!IsActive && !HasLoggedInBefore())
                Activate();

            LastLoginAt = DateTime.UtcNow;
        }
    }
}
