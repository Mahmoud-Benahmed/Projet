using ERP.AuthService.Application.DTOs.Role;
using ERP.AuthService.Application.Interfaces.Repositories;
using ERP.AuthService.Application.Interfaces.Services;
using ERP.AuthService.Application.Services;
using ERP.AuthService.Domain;
using FluentAssertions;
using Moq;

namespace ERP.AuthService.Tests.Unit.Services
{ // =========================
    // CONTROLE SERVICE
    // =========================
    public class ControleServiceTests
    {
        private readonly Mock<IControleRepository> _repoMock = new();
        private readonly ControleService _service;

        public ControleServiceTests()
        {
            _service = new ControleService(_repoMock.Object);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllControles()
        {
            var controles = new List<Controle>
            {
                new Controle("UserManagement", "ViewUsers", "Can view users"),
                new Controle("UserManagement", "EditUsers", "Can edit users"),
                new Controle("Reporting", "ViewReports", "Can view reports")
            };
            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(controles);

            var result = await _service.GetAllAsync();

            result.Should().HaveCount(3);
        }

        [Fact]
        public async Task GetAllAsync_EmptyControles_ShouldReturnEmptyList()
        {
            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Controle>());

            var result = await _service.GetAllAsync();

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ShouldReturnControle()
        {
            var controle = new Controle("UserManagement", "ViewUsers", "Can view users");
            _repoMock.Setup(r => r.GetByIdAsync(controle.Id)).ReturnsAsync(controle);

            var result = await _service.GetByIdAsync(controle.Id);

            result.Should().NotBeNull();
            result.Libelle.Should().Be("ViewUsers");
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ShouldThrowException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Controle?)null);

            Func<Task> act = () => _service.GetByIdAsync(Guid.NewGuid());

            await act.Should().ThrowAsync<Exception>();
        }

        [Fact]
        public async Task GetByCategoryAsync_ExistingCategory_ShouldReturnControles()
        {
            var controles = new List<Controle>
            {
                new Controle("UserManagement", "ViewUsers", "Can view users"),
                new Controle("UserManagement", "EditUsers", "Can edit users")
            };
            _repoMock.Setup(r => r.GetByCategoryAsync("UserManagement")).ReturnsAsync(controles);

            var result = await _service.GetByCategoryAsync("UserManagement");

            result.Should().HaveCount(2);
            result.All(c => c.Category == "UserManagement").Should().BeTrue();
        }

        [Fact]
        public async Task GetByCategoryAsync_NonExistingCategory_ShouldReturnEmptyList()
        {
            _repoMock.Setup(r => r.GetByCategoryAsync(It.IsAny<string>()))
                     .ReturnsAsync(new List<Controle>());

            var result = await _service.GetByCategoryAsync("Unknown");

            result.Should().BeEmpty();
        }
    }
}