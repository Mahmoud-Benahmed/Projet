using ERP.AuthService.Domain;
using FluentAssertions;

namespace ERP.AuthService.Tests.Unit.Domain
{
    public class PrivilegeTests
    {
        // =========================
        // CONSTRUCTOR
        // =========================
        [Fact]
        public void Constructor_ValidInputs_ShouldCreatePrivilege()
        {
            var roleId = Guid.NewGuid();
            var controleId = Guid.NewGuid();

            var privilege = new Privilege(roleId, controleId, true);

            privilege.Id.Should().NotBeEmpty();
            privilege.RoleId.Should().Be(roleId);
            privilege.ControleId.Should().Be(controleId);
            privilege.IsGranted.Should().BeTrue();
        }

        [Fact]
        public void Constructor_IsGrantedFalse_ShouldCreateDeniedPrivilege()
        {
            var privilege = new Privilege(Guid.NewGuid(), Guid.NewGuid(), false);
            privilege.IsGranted.Should().BeFalse();
        }

        [Fact]
        public void Constructor_TwoPrivileges_ShouldHaveDifferentIds()
        {
            var p1 = new Privilege(Guid.NewGuid(), Guid.NewGuid(), true);
            var p2 = new Privilege(Guid.NewGuid(), Guid.NewGuid(), true);
            p1.Id.Should().NotBe(p2.Id);
        }

        // =========================
        // SET GRANTED
        // =========================
        [Fact]
        public void SetGranted_True_ShouldGrantPrivilege()
        {
            var privilege = new Privilege(Guid.NewGuid(), Guid.NewGuid(), false);
            privilege.SetGranted(true);
            privilege.IsGranted.Should().BeTrue();
        }

        [Fact]
        public void SetGranted_False_ShouldDenyPrivilege()
        {
            var privilege = new Privilege(Guid.NewGuid(), Guid.NewGuid(), true);
            privilege.SetGranted(false);
            privilege.IsGranted.Should().BeFalse();
        }

        [Fact]
        public void SetGranted_SameValue_ShouldKeepValue()
        {
            var privilege = new Privilege(Guid.NewGuid(), Guid.NewGuid(), true);
            privilege.SetGranted(true);
            privilege.IsGranted.Should().BeTrue();
        }
    }
}