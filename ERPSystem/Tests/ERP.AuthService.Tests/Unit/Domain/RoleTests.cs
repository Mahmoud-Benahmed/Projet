using ERP.AuthService.Domain;
using FluentAssertions;

namespace ERP.AuthService.Tests.Unit.Domain
{
    public class RoleTests
    {
        // =========================
        // CONSTRUCTOR
        // =========================
        [Fact]
        public void Constructor_ValidLibelle_ShouldCreateRole()
        {
            var role = new Role(RoleEnum.SystemAdmin);

            role.Id.Should().NotBeEmpty();
            role.Libelle.Should().Be(RoleEnum.SystemAdmin);
        }

        [Theory]
        [InlineData(RoleEnum.SystemAdmin)]
        [InlineData(RoleEnum.SalesManager)]
        public void Constructor_AnyRoleEnum_ShouldCreateRole(RoleEnum libelle)
        {
            var role = new Role(libelle);
            role.Libelle.Should().Be(libelle);
        }

        [Fact]
        public void Constructor_TwoRoles_ShouldHaveDifferentIds()
        {
            var r1 = new Role(RoleEnum.SystemAdmin);
            var r2 = new Role(RoleEnum.SalesManager);
            r1.Id.Should().NotBe(r2.Id);
        }
    }
}