using ERP.AuthService.Application.DTOs.AuditLog;
using ERP.AuthService.Application.DTOs.AuthUser;
using ERP.AuthService.Application.Interfaces.Services;
using ERP.AuthService.Controllers;
using ERP.AuthService.Domain.Logger;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ERP.AuthService.Tests.Integration.Controllers
{
    public class AuditLogControllerTests
    {
        private readonly Mock<IAuditLogService> _serviceMock = new();
        private readonly AuditLogController _controller;

        public AuditLogControllerTests()
        {
            _controller = new AuditLogController(_serviceMock.Object);
        }

        private static AuditLogResponseDto MakeDto(AuditAction action = AuditAction.Login)
            => new(Guid.NewGuid(), action, Guid.NewGuid(), null,
                   true, null, "::1", "TestAgent", null, DateTime.UtcNow);

        private static PagedResultDto<AuditLogResponseDto> MakePaged(List<AuditLogResponseDto> items)
            => new(items, items.Count, 1, 20);

        // =========================
        // GET ALL
        // =========================
        [Fact]
        public async Task GetAll_ShouldReturn200WithPagedResult()
        {
            var items = new List<AuditLogResponseDto> { MakeDto(), MakeDto(AuditAction.Logout) };
            _serviceMock.Setup(s => s.GetAllAsync(1, 20)).ReturnsAsync(MakePaged(items));

            var result = await _controller.GetAll(1, 20);

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeAssignableTo<PagedResultDto<AuditLogResponseDto>>();
        }

        [Fact]
        public async Task GetAll_EmptyLogs_ShouldReturn200WithEmptyList()
        {
            _serviceMock.Setup(s => s.GetAllAsync(1, 20))
                        .ReturnsAsync(MakePaged(new List<AuditLogResponseDto>()));

            var result = await _controller.GetAll(1, 20);

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            var body = ok.Value.Should().BeAssignableTo<PagedResultDto<AuditLogResponseDto>>().Subject;
            body.Items.Should().BeEmpty();
        }

        // =========================
        // GET BY USER
        // =========================
        [Fact]
        public async Task GetByUser_ExistingUserId_ShouldReturn200()
        {
            var userId = Guid.NewGuid();
            var items = new List<AuditLogResponseDto> { MakeDto() };
            _serviceMock.Setup(s => s.GetByUserAsync(userId, 1, 20)).ReturnsAsync(MakePaged(items));

            var result = await _controller.GetByUser(userId, 1, 20);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetByUser_NonExistingUserId_ShouldReturn200WithEmptyList()
        {
            _serviceMock.Setup(s => s.GetByUserAsync(It.IsAny<Guid>(), 1, 20))
                        .ReturnsAsync(MakePaged(new List<AuditLogResponseDto>()));

            var result = await _controller.GetByUser(Guid.NewGuid(), 1, 20);

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            var body = ok.Value.Should().BeAssignableTo<PagedResultDto<AuditLogResponseDto>>().Subject;
            body.Items.Should().BeEmpty();
        }

        // =========================
        // COUNT
        // =========================
        [Fact]
        public async Task Count_ShouldReturn200WithCount()
        {
            _serviceMock.Setup(s => s.CountAsync()).ReturnsAsync(42);

            var result = await _controller.Count();

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().Be(42L);
        }

        [Fact]
        public async Task Count_EmptyLogs_ShouldReturn200WithZero()
        {
            _serviceMock.Setup(s => s.CountAsync()).ReturnsAsync(0);

            var result = await _controller.Count();

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().Be(0L);
        }

        // =========================
        // CLEAR
        // =========================
        [Fact]
        public async Task Clear_ShouldReturn204()
        {
            _serviceMock.Setup(s => s.ClearAsync()).Returns(Task.CompletedTask);

            var result = await _controller.Clear();

            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task Clear_ShouldCallServiceClear()
        {
            _serviceMock.Setup(s => s.ClearAsync()).Returns(Task.CompletedTask);

            await _controller.Clear();

            _serviceMock.Verify(s => s.ClearAsync(), Times.Once);
        }
    }
}