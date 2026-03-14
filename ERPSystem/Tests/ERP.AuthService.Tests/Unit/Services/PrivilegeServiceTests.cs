using ERP.AuthService.Application.DTOs.Role;
using ERP.AuthService.Application.Exceptions.Role;
using ERP.AuthService.Application.Interfaces.Repositories;
using ERP.AuthService.Application.Interfaces.Services;
using ERP.AuthService.Application.Services;
using ERP.AuthService.Domain;
using FluentAssertions;
using Moq;

namespace ERP.AuthService.Tests.Unit.Services
{
    // =========================
    // PRIVILEGE SERVICE
    // =========================
    public class PrivilegeServiceTests
    {
        private readonly Mock<IPrivilegeRepository> _repoMock = new();
        private readonly Mock<IRoleRepository> _roleRepoMock = new();
        private readonly Mock<IControleRepository> _controleRepoMock = new();
        private readonly PrivilegeService _service;

        public PrivilegeServiceTests()
        {
            _service = new PrivilegeService(
                _repoMock.Object,
                _controleRepoMock.Object,
                _roleRepoMock.Object
                );
        }

        private Role MakeRole() => new Role(RoleEnum.SalesManager);
        private Controle MakeControle() => new Controle("UserManagement", "ViewUsers", "Can view users");

        [Fact]
        public async Task GetByRoleIdAsync_ExistingRole_ShouldReturnPrivileges()
        {
            // Arrange
            var roleId = Guid.NewGuid();
            var controleId = Guid.NewGuid();

            // Mock role exists
            var role = new Role(RoleEnum.SystemAdmin);
            _roleRepoMock.Setup(r => r.GetByIdAsync(roleId)).ReturnsAsync(role);

            // Mock privileges
            var privileges = new List<Privilege>
            {
                new Privilege(roleId, controleId, true)
            };
            _repoMock.Setup(r => r.GetByRoleIdAsync(roleId)).ReturnsAsync(privileges);

            // Act
            var result = await _service.GetByRoleIdAsync(roleId);

            // Assert
            result.Should().HaveCount(1);
            result[0].RoleId.Should().Be(roleId);
        }

        [Fact]
        public async Task GetByRoleIdAsync_NonExistingRole_ShouldThrowRoleNotFoundException()
        {
            var roleId = Guid.NewGuid();

            _roleRepoMock
                .Setup(r => r.GetByIdAsync(roleId))
                .ReturnsAsync((Role?)null);

            Func<Task> act = () => _service.GetByRoleIdAsync(roleId);

            await act.Should().ThrowAsync<RoleNotFoundException>();
        }

        [Fact]
        public async Task AllowAsync_ExistingPrivilege_ShouldGrantPrivilege()
        {
            var role = MakeRole();
            var controle = MakeControle();
            var privilege = new Privilege(role.Id, controle.Id, false);

            _roleRepoMock.Setup(r => r.GetByIdAsync(role.Id)).ReturnsAsync(role);
            _controleRepoMock.Setup(r => r.GetByIdAsync(controle.Id)).ReturnsAsync(controle);
            _repoMock.Setup(r => r.GetByRoleIdAndControleIdAsync(role.Id, controle.Id))
                     .ReturnsAsync(privilege);

            await _service.AllowAsync(role.Id, controle.Id);

            privilege.IsGranted.Should().BeTrue();
            _repoMock.Verify(r => r.UpdateAsync(privilege), Times.Once);
        }

        [Fact]
        public async Task DenyAsync_ExistingPrivilege_ShouldDenyPrivilege()
        {
            var role = MakeRole();
            var controle = MakeControle();
            var privilege = new Privilege(role.Id, controle.Id, true);

            _roleRepoMock.Setup(r => r.GetByIdAsync(role.Id)).ReturnsAsync(role);
            _controleRepoMock.Setup(r => r.GetByIdAsync(controle.Id)).ReturnsAsync(controle);
            _repoMock.Setup(r => r.GetByRoleIdAndControleIdAsync(role.Id, controle.Id))
                     .ReturnsAsync(privilege);

            await _service.DenyAsync(role.Id, controle.Id);

            privilege.IsGranted.Should().BeFalse();
            _repoMock.Verify(r => r.UpdateAsync(privilege), Times.Once);
        }

        [Fact]
        public async Task AllowAsync_NonExistingRole_ShouldThrowException()
        {
            _roleRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Role?)null);

            Func<Task> act = () => _service.AllowAsync(Guid.NewGuid(), Guid.NewGuid());

            await act.Should().ThrowAsync<Exception>();
        }

        [Fact]
        public async Task DenyAsync_NonExistingControle_ShouldThrowException()
        {
            var role = MakeRole();
            _roleRepoMock.Setup(r => r.GetByIdAsync(role.Id)).ReturnsAsync(role);
            _controleRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Controle?)null);

            Func<Task> act = () => _service.DenyAsync(role.Id, Guid.NewGuid());

            await act.Should().ThrowAsync<ControleNotFoundException>();
        }
    }
}