using ERP.AuthService.Application.DTOs.Role;
using ERP.AuthService.Application.Interfaces.Repositories;
using ERP.AuthService.Application.Interfaces.Services;
using ERP.AuthService.Application.Services;
using ERP.AuthService.Domain;
using FluentAssertions;
using Moq;

namespace ERP.AuthService.Tests.Unit.Services
{
    // =========================
    // ROLE SERVICE
    // =========================
    public class RoleServiceTests
    {
        private readonly Mock<IRoleRepository> _repoMock = new();
        private readonly RoleService _service;

        public RoleServiceTests()
        {
            _service = new RoleService(_repoMock.Object);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllRoles()
        {
            var roles = new List<Role>
            {
                new Role(RoleEnum.SystemAdmin),
                new Role(RoleEnum.Accountant)
            };
            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(roles);

            var result = await _service.GetAllAsync();

            result.Should().HaveCount(2);
            result.Select(r => r.Libelle).Should().Contain(RoleEnum.SystemAdmin);
        }

        [Fact]
        public async Task GetAllAsync_EmptyRoles_ShouldReturnEmptyList()
        {
            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Role>());

            var result = await _service.GetAllAsync();

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ShouldReturnRole()
        {
            var role = new Role(RoleEnum.SystemAdmin);
            _repoMock.Setup(r => r.GetByIdAsync(role.Id)).ReturnsAsync(role);

            var result = await _service.GetByIdAsync(role.Id);

            result.Should().NotBeNull();
            result.Libelle.Should().Be(RoleEnum.SystemAdmin);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ShouldThrowRoleNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Role?)null);

            Func<Task> act = () => _service.GetByIdAsync(Guid.NewGuid());

            await act.Should().ThrowAsync<Exception>()
                     .WithMessage("*not found*");
        }
    }
}