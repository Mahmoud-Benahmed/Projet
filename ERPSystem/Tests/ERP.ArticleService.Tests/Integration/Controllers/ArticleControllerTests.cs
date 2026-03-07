using ERP.ArticleService.Application.Interfaces;
using ERP.ArticleService.Application.DTOs;
using ERP.ArticleService.Domain;
using ERP.ArticleService.API.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ERP.ArticleService.Tests.Integration.Controllers
{
    public class ArticleControllerTests
    {
        private readonly Mock<IArticleService> _serviceMock = new();
        private readonly ArticleController _controller;
        private readonly Category _category = new("Électronique", 19m);

        public ArticleControllerTests()
        {
            _controller = new ArticleController(_serviceMock.Object);
        }

        // =========================
        // GET ALL
        // =========================
        [Fact]
        public async Task GetAll_ShouldReturn200WithArticles()
        {
            var articles = new List<Article>
            {
                new("ART-2026-000001", "Écran", 100m, _category, "1234567890128", 19m),
                new("ART-2026-000002", "Clavier", 50m, _category, "9876543210987", 19m)
            };
            _serviceMock.Setup(s => s.GetAllAsync()).ReturnsAsync(articles);

            var result = await _controller.GetAll();

            var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var body = ok.Value.Should().BeAssignableTo<List<Article>>().Subject;
            body.Should().HaveCount(2);
        }

        // =========================
        // GET BY ID
        // =========================
        [Fact]
        public async Task GetById_ExistingId_ShouldReturn200()
        {
            var article = new Article("ART-2026-000001", "Écran", 100m, _category, "1234567890128", 19m);
            _serviceMock.Setup(s => s.GetByIdAsync(article.Id)).ReturnsAsync(article);

            var result = await _controller.GetById(article.Id);

            result.Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetById_NonExistingId_ShouldReturn404()
        {
            _serviceMock.Setup(s => s.GetByIdAsync(It.IsAny<Guid>()))
                        .ThrowsAsync(new KeyNotFoundException("Not found"));

            var result = await _controller.GetById(Guid.NewGuid());

            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        // =========================
        // GET BY CODE
        // =========================
        [Fact]
        public async Task GetByCode_ExistingCode_ShouldReturn200()
        {
            var article = new Article("ART-2026-000001", "Écran", 100m, _category, "1234567890128", 19m);
            _serviceMock.Setup(s => s.GetByCodeAsync("ART-2026-000001")).ReturnsAsync(article);

            var result = await _controller.GetByCode("ART-2026-000001");

            result.Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetByCode_NonExistingCode_ShouldReturn404()
        {
            _serviceMock.Setup(s => s.GetByCodeAsync(It.IsAny<string>()))
                        .ThrowsAsync(new KeyNotFoundException("Not found"));

            var result = await _controller.GetByCode("UNKNOWN");

            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        // =========================
        // CREATE
        // =========================
        [Fact]
        public async Task Create_ValidRequest_ShouldReturn201()
        {
            var article = new Article("ART-2026-000001", "Écran", 100m, _category, "1234567890128", 19m);
            _serviceMock.Setup(s => s.CreateAsync(It.IsAny<CreateArticleRequestDto>()))
                        .ReturnsAsync(article);

            var request = new CreateArticleRequestDto("Écran", 100m, _category.Id, "1234567890128", 19m);
            var result = await _controller.Create(request);

            result.Result.Should().BeOfType<CreatedAtActionResult>();
        }

        [Fact]
        public async Task Create_CategoryNotFound_ShouldReturn404()
        {
            _serviceMock.Setup(s => s.CreateAsync(It.IsAny<CreateArticleRequestDto>()))
                        .ThrowsAsync(new KeyNotFoundException("Category not found"));

            var request = new CreateArticleRequestDto("Écran", 100m, Guid.NewGuid(), "1234567890128", 19m);
            var result = await _controller.Create(request);

            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task Create_InvalidArgument_ShouldReturn400()
        {
            _serviceMock.Setup(s => s.CreateAsync(It.IsAny<CreateArticleRequestDto>()))
                        .ThrowsAsync(new ArgumentException("Prix must be positive"));

            var request = new CreateArticleRequestDto("Écran", -1m, _category.Id, "1234567890128", 19m);
            var result = await _controller.Create(request);

            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        // =========================
        // UPDATE
        // =========================
        [Fact]
        public async Task Update_ValidRequest_ShouldReturn200()
        {
            var article = new Article("ART-2026-000001", "Écran 32 pouces", 1599.99m, _category, "9876543210987", 15m);
            _serviceMock.Setup(s => s.UpdateAsync(article.Id, It.IsAny<UpdateArticleRequestDto>()))
                        .ReturnsAsync(article);

            var request = new UpdateArticleRequestDto("Écran 32 pouces", 1599.99m, _category.Id, "9876543210987", 15m);
            var result = await _controller.Update(article.Id, request);

            result.Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task Update_NotFound_ShouldReturn404()
        {
            _serviceMock.Setup(s => s.UpdateAsync(It.IsAny<Guid>(), It.IsAny<UpdateArticleRequestDto>()))
                        .ThrowsAsync(new KeyNotFoundException("Not found"));

            var request = new UpdateArticleRequestDto("Écran", 100m, Guid.NewGuid(), null, null);
            var result = await _controller.Update(Guid.NewGuid(), request);

            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        // =========================
        // ACTIVATE / DEACTIVATE
        // =========================
        [Fact]
        public async Task Activate_ExistingArticle_ShouldReturn204()
        {
            var id = Guid.NewGuid();
            _serviceMock.Setup(s => s.ActivateAsync(id)).Returns(Task.CompletedTask);

            var result = await _controller.Activate(id);

            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task Activate_NonExistingArticle_ShouldReturn404()
        {
            _serviceMock.Setup(s => s.ActivateAsync(It.IsAny<Guid>()))
                        .ThrowsAsync(new KeyNotFoundException("Not found"));

            var result = await _controller.Activate(Guid.NewGuid());

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task Deactivate_ExistingArticle_ShouldReturn204()
        {
            var id = Guid.NewGuid();
            _serviceMock.Setup(s => s.DeactivateAsync(id)).Returns(Task.CompletedTask);

            var result = await _controller.Deactivate(id);

            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task Deactivate_NonExistingArticle_ShouldReturn404()
        {
            _serviceMock.Setup(s => s.DeactivateAsync(It.IsAny<Guid>()))
                        .ThrowsAsync(new KeyNotFoundException("Not found"));

            var result = await _controller.Deactivate(Guid.NewGuid());

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        // =========================
        // DELETE
        // =========================
        [Fact]
        public async Task Delete_ExistingArticle_ShouldReturn204()
        {
            var id = Guid.NewGuid();
            _serviceMock.Setup(s => s.DeleteAsync(id)).Returns(Task.CompletedTask);

            var result = await _controller.Delete(id);

            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task Delete_NonExistingArticle_ShouldReturn404()
        {
            _serviceMock.Setup(s => s.DeleteAsync(It.IsAny<Guid>()))
                        .ThrowsAsync(new KeyNotFoundException("Not found"));

            var result = await _controller.Delete(Guid.NewGuid());

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        // =========================
        // PAGING
        // =========================
        [Fact]
        public async Task GetPagedByCategory_ValidInputs_ShouldReturn200()
        {
            var paged = new PagedResultDto<Article>(
                new List<Article> { new("ART-2026-000001", "Écran", 100m, _category, "1234567890128", 19m) },
                1, 1, 10);
            _serviceMock.Setup(s => s.GetPagedByCategoryIdAsync(_category.Id, 1, 10))
                        .ReturnsAsync(paged);

            var result = await _controller.GetPagedByCategory(_category.Id, 1, 10);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetPagedByStatus_ValidInputs_ShouldReturn200()
        {
            var paged = new PagedResultDto<Article>(
                new List<Article> { new("ART-2026-000001", "Écran", 100m, _category, "1234567890128", 19m) },
                1, 1, 10);
            _serviceMock.Setup(s => s.GetPagedByStatusAsync(true, 1, 10)).ReturnsAsync(paged);

            var result = await _controller.GetPagedByStatus(true, 1, 10);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetPagedByLibelle_ValidFilter_ShouldReturn200()
        {
            var paged = new PagedResultDto<Article>(
                new List<Article> { new("ART-2026-000001", "Écran 27 pouces", 100m, _category, "1234567890128", 19m) },
                1, 1, 10);
            _serviceMock.Setup(s => s.GetPagedByLibelleAsync("Écran", 1, 10)).ReturnsAsync(paged);

            var result = await _controller.GetPagedByLibelle("Écran", 1, 10);

            result.Should().BeOfType<OkObjectResult>();
        }
    }
}