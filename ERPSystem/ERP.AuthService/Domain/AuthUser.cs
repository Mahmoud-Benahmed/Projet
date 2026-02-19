using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;


namespace ERP.AuthService.Domain
{
    public class AuthUser
    {
        [BsonId]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid Id { get; private set; }

        public string Email { get; set; }= "";

        public string PasswordHash { get; set; }= "";

        public bool MustChangePassword { get; private set; } = true;

        public bool IsActive { get; private set; } = true;

        public DateTime CreatedAt { get; private set; }

        public DateTime? LastLoginAt { get; private set; }

        [BsonRepresentation(BsonType.String)]
        public UserRole Role { get; set; }

        private AuthUser() { }

        public AuthUser(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email is required");


            Id = Guid.NewGuid();
            Email = email;
            CreatedAt = DateTime.UtcNow;
        }

        public void SetPasswordHash(string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(passwordHash))
                throw new ArgumentException("Password hash is required");
            PasswordHash = passwordHash;
        }
        public void SetRole(UserRole role)
        {
            if (!Enum.IsDefined(typeof(UserRole), role))
                throw new ArgumentException($"Invalid role: {role}");
            Role = role;
        }

        public void Deactivate()
        {
            IsActive = false;
        }

        public void Activate()
        {
            IsActive = true;
        }

        public void ChangePassword(string newPasswordHash)
        {
            if (string.IsNullOrWhiteSpace(newPasswordHash))
                throw new ArgumentException("Password hash is required");

            PasswordHash = newPasswordHash;
            MustChangePassword = false;
        }

        public void ForcePasswordChange()
        {
            MustChangePassword = true;
        }

        public void RecordLogin()
        {
            LastLoginAt = DateTime.UtcNow;
        }
    }
}
