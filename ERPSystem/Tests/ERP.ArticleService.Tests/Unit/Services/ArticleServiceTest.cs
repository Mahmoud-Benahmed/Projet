using ERP.ArticleService.Application.DTOs;
using ERP.ArticleService.Application.Interfaces;
using ERP.ArticleService.Domain;
using FluentAssertions;
using ArticleServiceClass = ERP.ArticleService.Application.Services.ArticleService;
using Moq;
using System.Timers;

namespace ERP.ArticleService.Tests.Unit.Services
{
    public class ArticleServiceTests
    {
        private readonly Mock<IArticleRepository> _articleRepoMock = new();
        private readonly Mock<ICategoryRepository> _categoryRepoMock = new();
        private readonly Mock<IArticleCodeService> _codeServiceMock = new();
        private readonly ArticleServiceClass _service;

        private readonly Category _category = new("Électronique", 19m);

        public ArticleServiceTests()
        {
            _service = new ArticleServiceClass(
                _articleRepoMock.Object,
                _categoryRepoMock.Object,
                _codeServiceMock.Object);
        }

        // =========================
        // CREATE
        // =========================
        [Fact]
        public async Task CreateAsync_ValidRequest_ShouldCreateAndReturnArticle()
        {
            _categoryRepoMock.Setup(r => r.GetByIdAsync(_category.Id)).ReturnsAsync(_category);
            _codeServiceMock.Setup(s => s.GenerateArticleCodeAsync()).ReturnsAsync("ART-2026-000001");

            var request = new CreateArticleRequestDto(
                Libelle: "Écran 27 pouces",
                Prix: 1299.99m,
                CategoryId: _category.Id,
                BarCode: "1234567890128",
                TVA: 19m);

            var result = await _service.CreateAsync(request);

            result.Libelle.Should().Be("Écran 27 pouces");
            result.Prix.Should().Be(1299.99m);
            result.CodeRef.Should().Be("ART-2026-000001");
            result.BarCode.Should().Be("1234567890128");
            result.TVA.Should().Be(19m);
            _articleRepoMock.Verify(r => r.AddAsync(It.IsAny<Article>()), Times.Once);
            _articleRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_NullTVA_ShouldInheritCategoryTVA()
        {
            _categoryRepoMock.Setup(r => r.GetByIdAsync(_category.Id)).ReturnsAsync(_category);
            _codeServiceMock.Setup(s => s.GenerateArticleCodeAsync()).ReturnsAsync("ART-2026-000001");

            var request = new CreateArticleRequestDto(
                Libelle: "Écran 27 pouces",
                Prix: 1299.99m,
                CategoryId: _category.Id,
                BarCode: "1234567890128",
                TVA: null);

            var result = await _service.CreateAsync(request);

            result.TVA.Should().Be(_category.TVA);
        }

        [Fact]
        public async Task CreateAsync_CategoryNotFound_ShouldThrowKeyNotFoundException()
        {
            _categoryRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Category?)null);

            var request = new CreateArticleRequestDto(
                Libelle: "Écran",
                Prix: 100m,
                CategoryId: Guid.NewGuid(),
                BarCode: "1234567890128",
                TVA: 19m);

            Func<Task> act = () => _service.CreateAsync(request);

            await act.Should().ThrowAsync<KeyNotFoundException>()
                     .WithMessage("*not found*");
        }

        // =========================
        // GET BY ID
        // =========================
        [Fact]
        public async Task GetByIdAsync_ExistingId_ShouldReturnArticle()
        {
            var article = new Article("ART-2026-000001", "Écran", 100m, _category, "1234567890128", 19m);
            _articleRepoMock.Setup(r => r.GetByIdAsync(article.Id)).ReturnsAsync(article);

            var result = await _service.GetByIdAsync(article.Id);

            result.Should().BeEquivalentTo(article);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ShouldThrowKeyNotFoundException()
        {
            _articleRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Article?)null);

            Func<Task> act = () => _service.GetByIdAsync(Guid.NewGuid());

            await act.Should().ThrowAsync<KeyNotFoundException>()
                     .WithMessage("*not found*");
        }

        // =========================
        // GET BY CODE
        // =========================
        [Fact]
        public async Task GetByCodeAsync_ExistingCode_ShouldReturnArticle()
        {
            var article = new Article("ART-2026-000001", "Écran", 100m, _category, "1234567890128", 19m);
            _articleRepoMock.Setup(r => r.GetByCodeAsync("ART-2026-000001")).ReturnsAsync(article);

            var result = await _service.GetByCodeAsync("ART-2026-000001");

            result.Should().BeEquivalentTo(article);
        }

        [Fact]
        public async Task GetByCodeAsync_NonExistingCode_ShouldThrowKeyNotFoundException()
        {
            _articleRepoMock.Setup(r => r.GetByCodeAsync(It.IsAny<string>())).ReturnsAsync((Article?)null);

            Func<Task> act = () => _service.GetByCodeAsync("UNKNOWN");

            await act.Should().ThrowAsync<KeyNotFoundException>()
                     .WithMessage("*not found*");
        }

        // =========================
        // GET ALL
        // =========================
        [Fact]
        public async Task GetAllAsync_ShouldReturnAllArticles()
        {
            var articles = new List<Article>
            {
                new("ART-2026-000001", "Écran", 100m, _category, "1234567890128", 19m),
                new("ART-2026-000002", "Clavier", 50m, _category, "9876543210987", 19m)
            };
            _articleRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(articles);

            var result = await _service.GetAllAsync();

            result.Should().HaveCount(2);
        }

        // =========================
        // UPDATE
        // =========================
        [Fact]
        public async Task UpdateAsync_ValidRequest_ShouldUpdateAndReturn()
        {
            var article = new Article("ART-2026-000001", "Écran", 100m, _category, "1234567890128", 19m);
            var newCategory = new Category("Informatique", 15m);

            _articleRepoMock.Setup(r => r.GetByIdAsync(article.Id)).ReturnsAsync(article);
            _categoryRepoMock.Setup(r => r.GetByIdAsync(newCategory.Id)).ReturnsAsync(newCategory);

            var request = new UpdateArticleRequestDto(
                Libelle: "Écran 32 pouces",
                Prix: 1599.99m,
                CategoryId: newCategory.Id,
                BarCode: "9876543210987",
                TVA: 15m);

            var result = await _service.UpdateAsync(article.Id, request);

            result.Libelle.Should().Be("Écran 32 pouces");
            result.Prix.Should().Be(1599.99m);
            _articleRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ArticleNotFound_ShouldThrowKeyNotFoundException()
        {
            _articleRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Article?)null);

            var request = new UpdateArticleRequestDto("Écran", 100m, Guid.NewGuid(), null, null);

            Func<Task> act = () => _service.UpdateAsync(Guid.NewGuid(), request);

            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        // =========================
        // ACTIVATE / DEACTIVATE
        // =========================
        [Fact]
        public async Task ActivateAsync_ExistingArticle_ShouldActivate()
        {
            var article = new Article("ART-2026-000001", "Écran", 100m, _category, "1234567890128", 19m);
            article.Deactivate();
            _articleRepoMock.Setup(r => r.GetByIdAsync(article.Id)).ReturnsAsync(article);

            await _service.ActivateAsync(article.Id);

            article.IsActive.Should().BeTrue();
            _articleRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeactivateAsync_ExistingArticle_ShouldDeactivate()
        {
            var article = new Article("ART-2026-000001", "Écran", 100m, _category, "1234567890128", 19m);
            _articleRepoMock.Setup(r => r.GetByIdAsync(article.Id)).ReturnsAsync(article);

            await _service.DeactivateAsync(article.Id);

            article.IsActive.Should().BeFalse();
            _articleRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task ActivateAsync_NonExistingArticle_ShouldThrowKeyNotFoundException()
        {
            _articleRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Article?)null);

            Func<Task> act = () => _service.ActivateAsync(Guid.NewGuid());

            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        // =========================
        // DELETE
        // =========================
        [Fact]
        public async Task DeleteAsync_ExistingArticle_ShouldRemoveAndSave()
        {
            var article = new Article("ART-2026-000001", "Écran", 100m, _category, "1234567890128", 19m);
            _articleRepoMock.Setup(r => r.GetByIdAsync(article.Id)).ReturnsAsync(article);

            await _service.DeleteAsync(article.Id);

            _articleRepoMock.Verify(r => r.Remove(article), Times.Once);
            _articleRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        // =========================
        // PAGING
        // =========================
        [Fact]
        public async Task GetPagedByCategoryIdAsync_ValidInputs_ShouldReturnPagedResult()
        {
            var items = new List<Article>
            {
                new("ART-2026-000001", "Écran", 100m, _category, "1234567890128", 19m)
            };
            _articleRepoMock.Setup(r => r.GetPagedByCategoryIdAsync(_category.Id, 1, 10))
                            .ReturnsAsync((items, 1));

            var result = await _service.GetPagedByCategoryIdAsync(_category.Id, 1, 10);

            result.Items.Should().HaveCount(1);
            result.TotalCount.Should().Be(1);
        }

        [Fact]
        public async Task GetPagedByStatusAsync_ActiveArticles_ShouldReturnPagedResult()
        {
            var items = new List<Article>
            {
                new("ART-2026-000001", "Écran", 100m, _category, "1234567890128", 19m)
            };
            _articleRepoMock.Setup(r => r.GetPagedByStatusAsync(true, 1, 10))
                            .ReturnsAsync((items, 1));

            var result = await _service.GetPagedByStatusAsync(true, 1, 10);

            result.Items.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetPagedByLibelleAsync_ValidFilter_ShouldReturnPagedResult()
        {
            var items = new List<Article>
            {
                new("ART-2026-000001", "Écran 27 pouces", 100m, _category, "1234567890128", 19m)
            };
            _articleRepoMock.Setup(r => r.GetPagedByLibelleAsync("Écran", 1, 10))
                            .ReturnsAsync((items, 1));

            var result = await _service.GetPagedByLibelleAsync("Écran", 1, 10);

            result.Items.Should().HaveCount(1);
        }
    }
}