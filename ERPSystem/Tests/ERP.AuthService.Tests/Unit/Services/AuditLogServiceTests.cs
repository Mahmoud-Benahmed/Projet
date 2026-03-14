using ERP.AuthService.Application.DTOs.AuditLog;
using ERP.AuthService.Application.DTOs.AuthUser;
using ERP.AuthService.Application.Interfaces.Repositories;
using ERP.AuthService.Application.Services;
using ERP.AuthService.Domain.Logger;
using FluentAssertions;
using Moq;

namespace ERP.AuthService.Tests.Unit.Services
{
    public class AuditLogServiceTests
    {
        private readonly Mock<IAuditLogRepository> _repoMock = new();
        private readonly AuditLogService _service;

        public AuditLogServiceTests()
        {
            _service = new AuditLogService(_repoMock.Object);
        }

        private static AuditLog MakeLog(AuditAction action = AuditAction.Login, bool success = true)
            => new AuditLog(action, success, Guid.NewGuid(), null, null, "::1", "TestAgent", null);

        private static AuditLogResponseDto MakeDto(AuditLog log)
            => new(log.Id, log.Action, log.PerformedBy, log.TargetUserId,
                   log.Success, log.FailureReason, log.IpAddress,
                   log.UserAgent, log.Metadata, log.Timestamp);

        // =========================
        // GET ALL
        // =========================
        [Fact]
        public async Task GetAllAsync_ShouldReturnPagedResult()
        {
            var logs = new List<AuditLog> { MakeLog(), MakeLog(AuditAction.Logout) };
            _repoMock.Setup(r => r.GetAllAsync(1, 10)).ReturnsAsync(logs);
            _repoMock.Setup(r => r.CountAsync()).ReturnsAsync(2);

            var result = await _service.GetAllAsync(1, 10);

            result.Items.Should().HaveCount(2);
            result.TotalCount.Should().Be(2);
            result.PageNumber.Should().Be(1);
            result.PageSize.Should().Be(10);
        }

        [Fact]
        public async Task GetAllAsync_EmptyLogs_ShouldReturnEmptyPagedResult()
        {
            _repoMock.Setup(r => r.GetAllAsync(1, 10)).ReturnsAsync(new List<AuditLog>());
            _repoMock.Setup(r => r.CountAsync()).ReturnsAsync(0);

            var result = await _service.GetAllAsync(1, 10);

            result.Items.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
        }

        // =========================
        // GET BY USER
        // =========================
        [Fact]
        public async Task GetByUserAsync_ShouldReturnLogsForUser()
        {
            var userId = Guid.NewGuid();
            var logs = new List<AuditLog> { MakeLog(), MakeLog() };
            _repoMock.Setup(r => r.GetByUserAsync(userId, 1, 10)).ReturnsAsync(logs);

            var result = await _service.GetByUserAsync(userId, 1, 10);

            result.Items.Should().HaveCount(2);
            result.TotalCount.Should().Be(2);
        }

        [Fact]
        public async Task GetByUserAsync_NonExistingUser_ShouldReturnEmptyPagedResult()
        {
            _repoMock.Setup(r => r.GetByUserAsync(It.IsAny<Guid>(), 1, 10))
                     .ReturnsAsync(new List<AuditLog>());

            var result = await _service.GetByUserAsync(Guid.NewGuid(), 1, 10);

            result.Items.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
        }

        // =========================
        // COUNT
        // =========================
        [Fact]
        public async Task CountAsync_ShouldReturnTotalCount()
        {
            _repoMock.Setup(r => r.CountAsync()).ReturnsAsync(42);

            var result = await _service.CountAsync();

            result.Should().Be(42);
        }

        [Fact]
        public async Task CountAsync_EmptyLogs_ShouldReturnZero()
        {
            _repoMock.Setup(r => r.CountAsync()).ReturnsAsync(0);

            var result = await _service.CountAsync();

            result.Should().Be(0);
        }

        // =========================
        // CLEAR
        // =========================
        [Fact]
        public async Task ClearAsync_ShouldCallRepositoryClear()
        {
            _repoMock.Setup(r => r.ClearAsync()).Returns(Task.CompletedTask);

            await _service.ClearAsync();

            _repoMock.Verify(r => r.ClearAsync(), Times.Once);
        }
    }
}