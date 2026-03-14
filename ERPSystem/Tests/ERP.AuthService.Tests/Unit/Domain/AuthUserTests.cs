using ERP.AuthService.Domain;
using FluentAssertions;

namespace ERP.AuthService.Tests.Unit.Domain
{
    public class AuthUserTests
    {
        // =========================
        // CONSTRUCTOR
        // =========================
        [Fact]
        public void Constructor_ValidInputs_ShouldCreateAuthUser()
        {
            var user = new AuthUser("john_doe", "john@example.com", "John Doe");

            user.Id.Should().NotBeEmpty();
            user.Login.Should().Be("john_doe");
            user.Email.Should().Be("john@example.com");
            user.FullName.Should().Be("John Doe");
            user.IsActive.Should().BeTrue();
            user.MustChangePassword.Should().BeTrue();
            user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
            user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
            user.LastLoginAt.Should().BeNull();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Constructor_EmptyLogin_ShouldThrowArgumentException(string? login)
        {
            Action act = () => new AuthUser(login!, "john@example.com", "John Doe");
            act.Should().Throw<ArgumentException>()
               .WithMessage("*Username is required*");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Constructor_EmptyEmail_ShouldThrowArgumentException(string? email)
        {
            Action act = () => new AuthUser("john_doe", email!, "John Doe");
            act.Should().Throw<ArgumentException>()
               .WithMessage("*Email is required*");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Constructor_EmptyFullName_ShouldThrowException(string? fullName)
        {
            Action act = () => new AuthUser("john_doe", "john@example.com", fullName!);
            act.Should().Throw<Exception>();
        }

        // =========================
        // UPDATE PROFILE
        // =========================
        [Fact]
        public void UpdateProfile_ValidInputs_ShouldUpdate()
        {
            var user = new AuthUser("john_doe", "john@example.com", "John Doe");
            var before = user.UpdatedAt;

            user.UpdateProfile("Jane Doe", "jane@example.com");

            user.FullName.Should().Be("Jane Doe");
            user.Email.Should().Be("jane@example.com");
            user.UpdatedAt.Should().BeOnOrAfter(before);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void UpdateProfile_EmptyEmail_ShouldThrowArgumentException(string? email)
        {
            var user = new AuthUser("john_doe", "john@example.com", "John Doe");
            Action act = () => user.UpdateProfile("John Doe", email!);
            act.Should().Throw<ArgumentException>()
               .WithMessage("*Email is required*");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void UpdateProfile_EmptyFullName_ShouldThrowArgumentException(string? fullName)
        {
            var user = new AuthUser("john_doe", "john@example.com", "John Doe");
            Action act = () => user.UpdateProfile(fullName!, "john@example.com");
            act.Should().Throw<ArgumentException>()
               .WithMessage("*FullName is required*");
        }

        // =========================
        // PASSWORD
        // =========================
        [Fact]
        public void SetPasswordHash_ValidHash_ShouldSetPassword()
        {
            var user = new AuthUser("john_doe", "john@example.com", "John Doe");
            user.SetPasswordHash("hashed_password_123");
            user.PasswordHash.Should().Be("hashed_password_123");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void SetPasswordHash_EmptyHash_ShouldThrowArgumentException(string? hash)
        {
            var user = new AuthUser("john_doe", "john@example.com", "John Doe");
            Action act = () => user.SetPasswordHash(hash!);
            act.Should().Throw<ArgumentException>()
               .WithMessage("*Password hash is required*");
        }

        [Fact]
        public void ChangePassword_ValidHash_ShouldUpdatePasswordAndTimestamp()
        {
            var user = new AuthUser("john_doe", "john@example.com", "John Doe");
            user.SetPasswordHash("old_hash");
            var before = user.UpdatedAt;

            user.ChangePassword("new_hash");

            user.PasswordHash.Should().Be("new_hash");
            user.UpdatedAt.Should().BeOnOrAfter(before);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void ChangePassword_EmptyHash_ShouldThrowArgumentException(string? hash)
        {
            var user = new AuthUser("john_doe", "john@example.com", "John Doe");
            Action act = () => user.ChangePassword(hash!);
            act.Should().Throw<ArgumentException>()
               .WithMessage("*Password hash is required*");
        }

        // =========================
        // ROLE
        // =========================
        [Fact]
        public void SetRole_ValidRoleId_ShouldSetRoleAndUpdateTimestamp()
        {
            var user = new AuthUser("john_doe", "john@example.com", "John Doe");
            var roleId = Guid.NewGuid();
            var before = user.UpdatedAt;

            user.SetRole(roleId);

            user.RoleId.Should().Be(roleId);
            user.UpdatedAt.Should().BeOnOrAfter(before);
        }

        // =========================
        // ACTIVATE / DEACTIVATE
        // =========================
        [Fact]
        public void Deactivate_WhenActive_ShouldDeactivate()
        {
            var user = new AuthUser("john_doe", "john@example.com", "John Doe");
            user.Deactivate();
            user.IsActive.Should().BeFalse();
        }

        [Fact]
        public void Deactivate_WhenAlreadyInactive_ShouldRemainInactive()
        {
            var user = new AuthUser("john_doe", "john@example.com", "John Doe");
            user.Deactivate();
            user.Deactivate();
            user.IsActive.Should().BeFalse();
        }

        [Fact]
        public void Activate_WhenInactive_ShouldActivate()
        {
            var user = new AuthUser("john_doe", "john@example.com", "John Doe");
            user.Deactivate();
            user.Activate();
            user.IsActive.Should().BeTrue();
        }

        [Fact]
        public void Activate_WhenAlreadyActive_ShouldRemainActive()
        {
            var user = new AuthUser("john_doe", "john@example.com", "John Doe");
            user.Activate();
            user.IsActive.Should().BeTrue();
        }

        // =========================
        // LOGIN
        // =========================
        [Fact]
        public void CanLogin_WhenActive_ShouldReturnTrue()
        {
            var user = new AuthUser("john_doe", "john@example.com", "John Doe");
            user.CanLogin().Should().BeTrue();
        }

        [Fact]
        public void CanLogin_WhenInactive_ShouldReturnFalse()
        {
            var user = new AuthUser("john_doe", "john@example.com", "John Doe");
            user.Deactivate();
            user.CanLogin().Should().BeFalse();
        }

        [Fact]
        public void RecordLogin_ShouldSetLastLoginAt()
        {
            var user = new AuthUser("john_doe", "john@example.com", "John Doe");
            user.RecordLogin();
            user.LastLoginAt.Should().NotBeNull();
            user.LastLoginAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        }

        [Fact]
        public void HasLoggedInBefore_BeforeLogin_ShouldReturnFalse()
        {
            var user = new AuthUser("john_doe", "john@example.com", "John Doe");
            user.HasLoggedInBefore().Should().BeFalse();
        }

        [Fact]
        public void HasLoggedInBefore_AfterLogin_ShouldReturnTrue()
        {
            var user = new AuthUser("john_doe", "john@example.com", "John Doe");
            user.RecordLogin();
            user.HasLoggedInBefore().Should().BeTrue();
        }

        // =========================
        // FORCE PASSWORD CHANGE
        // =========================
        [Fact]
        public void ForcePasswordChange_ShouldSetMustChangePasswordTrue()
        {
            var user = new AuthUser("john_doe", "john@example.com", "John Doe");
            user.MustChangePassword = false;
            user.ForcePasswordChange();
            user.MustChangePassword.Should().BeTrue();
        }
    }
}