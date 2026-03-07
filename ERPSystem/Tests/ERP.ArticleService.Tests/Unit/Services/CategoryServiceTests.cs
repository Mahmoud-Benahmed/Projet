using ERP.ArticleService.Application.Interfaces;
using ERP.ArticleService.Application.Services;
using ERP.ArticleService.Domain;
using FluentAssertions;
using Moq;
using CategoryServiceClass = ERP.ArticleService.Application.Services.CategoryService;

namespace ERP.ArticleService.Tests.Unit.Services
{
    public class CategoryServiceTests
    {
        private readonly Mock<ICategoryRepository> _repoMock = new();
        private readonly CategoryServiceClass _service;

        public CategoryServiceTests()
        {
            _service = new CategoryServiceClass(_repoMock.Object);
        }

        // =========================
        // CREATE
        // =========================
        [Fact]
        public async Task CreateAsync_NewCategory_ShouldAddAndReturn()
        {
            _repoMock.Setup(r => r.GetByNameAsync("Électronique")).ReturnsAsync((Category?)null);

            var result = await _service.CreateAsync("Électronique", 19m);

            result.Name.Should().Be("Électronique");
            result.TVA.Should().Be(19m);
            _repoMock.Verify(r => r.AddAsync(It.IsAny<Category>()), Times.Once);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_DuplicateName_ShouldThrowInvalidOperationException()
        {
            var existing = new Category("Électronique", 19m);
            _repoMock.Setup(r => r.GetByNameAsync("Électronique")).ReturnsAsync(existing);

            Func<Task> act = () => _service.CreateAsync("Électronique", 19m);

            await act.Should().ThrowAsync<InvalidOperationException>()
                     .WithMessage("*already exists*");
        }

        // =========================
        // GET BY ID
        // =========================
        [Fact]
        public async Task GetByIdAsync_ExistingId_ShouldReturnCategory()
        {
            var category = new Category("Électronique", 19m);
            _repoMock.Setup(r => r.GetByIdAsync(category.Id)).ReturnsAsync(category);

            var result = await _service.GetByIdAsync(category.Id);

            result.Should().BeEquivalentTo(category);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ShouldThrowKeyNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Category?)null);

            Func<Task> act = () => _service.GetByIdAsync(Guid.NewGuid());

            await act.Should().ThrowAsync<KeyNotFoundException>()
                     .WithMessage("*not found*");
        }

        // =========================
        // GET BY NAME
        // =========================
        [Fact]
        public async Task GetByNameAsync_ExistingName_ShouldReturnCategory()
        {
            var category = new Category("Électronique", 19m);
            _repoMock.Setup(r => r.GetByNameAsync("Électronique")).ReturnsAsync(category);

            var result = await _service.GetByNameAsync("Électronique");

            result.Should().BeEquivalentTo(category);
        }

        [Fact]
        public async Task GetByNameAsync_NonExistingName_ShouldThrowKeyNotFoundException()
        {
            _repoMock.Setup(r => r.GetByNameAsync(It.IsAny<string>())).ReturnsAsync((Category?)null);

            Func<Task> act = () => _service.GetByNameAsync("Unknown");

            await act.Should().ThrowAsync<KeyNotFoundException>()
                     .WithMessage("*not found*");
        }

        // =========================
        // GET ALL
        // =========================
        [Fact]
        public async Task GetAllAsync_ShouldReturnAllCategories()
        {
            var categories = new List<Category>
            {
                new("Électronique", 19m),
                new("Informatique", 15m)
            };
            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(categories);

            var result = await _service.GetAllAsync();

            result.Should().HaveCount(2);
        }

        // =========================
        // UPDATE NAME
        // =========================
        [Fact]
        public async Task UpdateAsync_ValidInputs_ShouldUpdateAndReturn()
        {
            var category = new Category("Électronique", 19m);
            _repoMock.Setup(r => r.GetByIdAsync(category.Id)).ReturnsAsync(category);
            _repoMock.Setup(r => r.GetByNameAsync("Informatique")).ReturnsAsync((Category?)null);

            var result = await _service.UpdateAsync(category.Id, "Informatique", 15m);

            result.Name.Should().Be("Informatique");
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_DuplicateName_ShouldThrowInvalidOperationException()
        {
            var category = new Category("Électronique", 19m);
            var other = new Category("Informatique", 15m);
            _repoMock.Setup(r => r.GetByIdAsync(category.Id)).ReturnsAsync(category);
            _repoMock.Setup(r => r.GetByNameAsync("Informatique")).ReturnsAsync(other);

            Func<Task> act = () => _service.UpdateAsync(category.Id, "Informatique", 15m);

            await act.Should().ThrowAsync<InvalidOperationException>()
                     .WithMessage("*already exists*");
        }
        // =========================
        // DELETE
        // =========================
        [Fact]
        public async Task DeleteAsync_ExistingCategory_ShouldRemoveAndSave()
        {
            var category = new Category("Électronique", 19m);
            _repoMock.Setup(r => r.GetByIdAsync(category.Id)).ReturnsAsync(category);

            await _service.DeleteAsync(category.Id);

            _repoMock.Verify(r => r.Remove(category), Times.Once);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_NonExistingCategory_ShouldThrowKeyNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Category?)null);

            Func<Task> act = () => _service.DeleteAsync(Guid.NewGuid());

            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        // =========================
        // PAGING
        // =========================
        [Fact]
        public async Task GetPagedAsync_ValidPaging_ShouldReturnPagedResult()
        {
            var items = new List<Category> { new("Électronique", 19m) };
            _repoMock.Setup(r => r.GetPagedAsync(1, 10)).ReturnsAsync((items, 1));

            var result = await _service.GetPagedAsync(1, 10);

            result.Items.Should().HaveCount(1);
            result.TotalCount.Should().Be(1);
            result.PageNumber.Should().Be(1);
            result.PageSize.Should().Be(10);
        }

        [Theory]
        [InlineData(0, 10)]
        [InlineData(-1, 10)]
        [InlineData(1, 0)]
        [InlineData(1, -1)]
        public async Task GetPagedAsync_InvalidPaging_ShouldThrowArgumentOutOfRangeException(int pageNumber, int pageSize)
        {
            Func<Task> act = () => _service.GetPagedAsync(pageNumber, pageSize);
            await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
        }

        [Fact]
        public async Task GetPagedByNameAsync_EmptyFilter_ShouldThrowArgumentException()
        {
            Func<Task> act = () => _service.GetPagedByNameAsync("", 1, 10);
            await act.Should().ThrowAsync<ArgumentException>()
                     .WithMessage("*Name filter cannot be empty*");
        }

        [Fact]
        public async Task GetPagedByDateRangeAsync_FromAfterTo_ShouldThrowArgumentException()
        {
            Func<Task> act = () => _service.GetPagedByDateRangeAsync(
                DateTime.UtcNow.AddDays(1), DateTime.UtcNow, 1, 10);
            await act.Should().ThrowAsync<ArgumentException>()
                     .WithMessage("*'from' date must be earlier*");
        }

        // =========================
        // TVA FILTERING
        // =========================
        [Fact]
        public async Task GetBelowTVAAsync_ValidTVA_ShouldReturnCategories()
        {
            var categories = new List<Category> { new("Alimentaire", 5m) };
            _repoMock.Setup(r => r.GetBelowTVAAsync(10m)).ReturnsAsync(categories);

            var result = await _service.GetBelowTVAAsync(10m);

            result.Should().HaveCount(1);
            result[0].TVA.Should().BeLessThan(10m);
        }

        [Fact]
        public async Task GetBelowTVAAsync_ZeroOrNegative_ShouldThrowArgumentException()
        {
            Func<Task> act = () => _service.GetBelowTVAAsync(0m);
            await act.Should().ThrowAsync<ArgumentException>()
                     .WithMessage("*TVA must be greater than zero*");
        }

        [Fact]
        public async Task GetHigherThanTVAAsync_ValidTVA_ShouldReturnCategories()
        {
            var categories = new List<Category> { new("Luxe", 20m) };
            _repoMock.Setup(r => r.GetHigherThanTVAAsync(15m)).ReturnsAsync(categories);

            var result = await _service.GetHigherThanTVAAsync(15m);

            result.Should().HaveCount(1);
            result[0].TVA.Should().BeGreaterThan(15m);
        }

        [Fact]
        public async Task GetBetweenTVAAsync_ValidRange_ShouldReturnCategories()
        {
            var categories = new List<Category> { new("Électronique", 19m) };
            _repoMock.Setup(r => r.GetBetweenTVAAsync(10m, 20m)).ReturnsAsync(categories);

            var result = await _service.GetBetweenTVAAsync(10m, 20m);

            result.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetBetweenTVAAsync_MinGreaterThanMax_ShouldThrowArgumentException()
        {
            Func<Task> act = () => _service.GetBetweenTVAAsync(20m, 10m);
            await act.Should().ThrowAsync<ArgumentException>()
                     .WithMessage("*'min' TVA must be less than or equal to 'max'*");
        }
    }
}