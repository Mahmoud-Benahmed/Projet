using ERP.ArticleService.Domain;
using ERP.ArticleService.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ERP.ArticleService.Tests.Integration.Repositories
{
    public class CategoryRepositoryTests : IDisposable
    {
        private readonly ArticleDbContext _context;
        private readonly CategoryRepository _repository;

        public CategoryRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<ArticleDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ArticleDbContext(options);
            _repository = new CategoryRepository(_context);
        }

        public void Dispose() => _context.Dispose();

        // =========================
        // ADD & GET BY ID
        // =========================
        [Fact]
        public async Task AddAsync_ThenGetByIdAsync_ShouldReturnCategory()
        {
            var category = new Category("Électronique", 19m);
            await _repository.AddAsync(category);
            await _repository.SaveChangesAsync();

            var result = await _repository.GetByIdAsync(category.Id);

            result.Should().NotBeNull();
            result!.Name.Should().Be("Électronique");
            result.TVA.Should().Be(19m);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ShouldReturnNull()
        {
            var result = await _repository.GetByIdAsync(Guid.NewGuid());
            result.Should().BeNull();
        }

        // =========================
        // GET BY NAME
        // =========================
        [Fact]
        public async Task GetByNameAsync_ExistingName_ShouldReturnCategory()
        {
            var category = new Category("Électronique", 19m);
            await _repository.AddAsync(category);
            await _repository.SaveChangesAsync();

            var result = await _repository.GetByNameAsync("Électronique");

            result.Should().NotBeNull();
            result!.Name.Should().Be("Électronique");
        }

        [Fact]
        public async Task GetByNameAsync_NonExistingName_ShouldReturnNull()
        {
            var result = await _repository.GetByNameAsync("Unknown");
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByNameAsync_ShouldBeCaseInsensitive()
        {
            var category = new Category("Électronique", 19m);
            await _repository.AddAsync(category);
            await _repository.SaveChangesAsync();

            var result = await _repository.GetByNameAsync("électronique");

            result.Should().NotBeNull();
        }

        // =========================
        // GET ALL
        // =========================
        [Fact]
        public async Task GetAllAsync_ShouldReturnAllCategories()
        {
            await _repository.AddAsync(new Category("Électronique", 19m));
            await _repository.AddAsync(new Category("Informatique", 15m));
            await _repository.AddAsync(new Category("Mobilier", 10m));
            await _repository.SaveChangesAsync();

            var result = await _repository.GetAllAsync();

            result.Should().HaveCount(3);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllCategories_WithCorrectNames()
        {
            await _repository.AddAsync(new Category("Mobilier", 10m));
            await _repository.AddAsync(new Category("Électronique", 19m));
            await _repository.AddAsync(new Category("Informatique", 15m));
            await _repository.SaveChangesAsync();

            var result = await _repository.GetAllAsync();

            result.Should().HaveCount(3);
            result.Select(c => c.Name).Should().Contain(new[] { "Mobilier", "Électronique", "Informatique" });
        }

        // =========================
        // GET BY TVA
        // =========================
        [Fact]
        public async Task GetByTVAsync_ExistingTVA_ShouldReturnCategory()
        {
            var category = new Category("Électronique", 19m);
            await _repository.AddAsync(category);
            await _repository.SaveChangesAsync();

            var result = await _repository.GetByTVAsync(19m);

            result.Should().NotBeNull();
            result!.TVA.Should().Be(19m);
        }

        [Fact]
        public async Task GetByTVAsync_NonExistingTVA_ShouldReturnNull()
        {
            var result = await _repository.GetByTVAsync(99m);
            result.Should().BeNull();
        }

        // =========================
        // TVA FILTERING
        // =========================
        [Fact]
        public async Task GetBelowTVAAsync_ShouldReturnCategoriesWithTVABelow()
        {
            await _repository.AddAsync(new Category("Alimentaire", 5m));
            await _repository.AddAsync(new Category("Électronique", 19m));
            await _repository.AddAsync(new Category("Luxe", 25m));
            await _repository.SaveChangesAsync();

            var result = await _repository.GetBelowTVAAsync(10m);

            result.Should().HaveCount(1);
            result.All(c => c.TVA < 10m).Should().BeTrue();
        }

        [Fact]
        public async Task GetBelowTVAAsync_ShouldReturnOrderedByTVA()
        {
            await _repository.AddAsync(new Category("A", 3m));
            await _repository.AddAsync(new Category("B", 7m));
            await _repository.AddAsync(new Category("C", 5m));
            await _repository.SaveChangesAsync();

            var result = await _repository.GetBelowTVAAsync(10m);

            result.Select(c => c.TVA).Should().BeInAscendingOrder();
        }

        [Fact]
        public async Task GetHigherThanTVAAsync_ShouldReturnCategoriesWithTVAAbove()
        {
            await _repository.AddAsync(new Category("Alimentaire", 5m));
            await _repository.AddAsync(new Category("Électronique", 19m));
            await _repository.AddAsync(new Category("Luxe", 25m));
            await _repository.SaveChangesAsync();

            var result = await _repository.GetHigherThanTVAAsync(15m);

            result.Should().HaveCount(2);
            result.All(c => c.TVA > 15m).Should().BeTrue();
        }

        [Fact]
        public async Task GetBetweenTVAAsync_ShouldReturnCategoriesInRange()
        {
            await _repository.AddAsync(new Category("Alimentaire", 5m));
            await _repository.AddAsync(new Category("Électronique", 19m));
            await _repository.AddAsync(new Category("Luxe", 25m));
            await _repository.SaveChangesAsync();

            var result = await _repository.GetBetweenTVAAsync(10m, 20m);

            result.Should().HaveCount(1);
            result[0].TVA.Should().Be(19m);
        }

        [Fact]
        public async Task GetBetweenTVAAsync_ShouldBeInclusive()
        {
            await _repository.AddAsync(new Category("A", 10m));
            await _repository.AddAsync(new Category("B", 15m));
            await _repository.AddAsync(new Category("C", 20m));
            await _repository.SaveChangesAsync();

            var result = await _repository.GetBetweenTVAAsync(10m, 20m);

            result.Should().HaveCount(3);
        }

        // =========================
        // REMOVE
        // =========================
        [Fact]
        public async Task Remove_ExistingCategory_ShouldDeleteFromDatabase()
        {
            var category = new Category("Électronique", 19m);
            await _repository.AddAsync(category);
            await _repository.SaveChangesAsync();

            _repository.Remove(category);
            await _repository.SaveChangesAsync();

            var result = await _repository.GetByIdAsync(category.Id);
            result.Should().BeNull();
        }

        // =========================
        // PAGING
        // =========================
        [Fact]
        public async Task GetPagedAsync_ShouldReturnCorrectPage()
        {
            for (int i = 1; i <= 15; i++)
                await _repository.AddAsync(new Category($"Category {i:D2}", 10m));
            await _repository.SaveChangesAsync();

            var (items, totalCount) = await _repository.GetPagedAsync(1, 10);

            items.Should().HaveCount(10);
            totalCount.Should().Be(15);
        }

        [Fact]
        public async Task GetPagedAsync_SecondPage_ShouldReturnRemainingItems()
        {
            for (int i = 1; i <= 15; i++)
                await _repository.AddAsync(new Category($"Category {i:D2}", 10m));
            await _repository.SaveChangesAsync();

            var (items, totalCount) = await _repository.GetPagedAsync(2, 10);

            items.Should().HaveCount(5);
            totalCount.Should().Be(15);
        }

        [Fact]
        public async Task GetPagedByNameAsync_ShouldFilterByName()
        {
            await _repository.AddAsync(new Category("Électronique", 19m));
            await _repository.AddAsync(new Category("Électroménager", 10m));
            await _repository.AddAsync(new Category("Mobilier", 5m));
            await _repository.SaveChangesAsync();

            var (items, totalCount) = await _repository.GetPagedByNameAsync("Électro", 1, 10);

            items.Should().HaveCount(2);
            totalCount.Should().Be(2);
        }

        [Fact]
        public async Task GetPagedByDateRangeAsync_ShouldFilterByDateRange()
        {
            await _repository.AddAsync(new Category("Électronique", 19m));
            await _repository.SaveChangesAsync();

            var from = DateTime.UtcNow.AddMinutes(-1);
            var to = DateTime.UtcNow.AddMinutes(1);

            var (items, totalCount) = await _repository.GetPagedByDateRangeAsync(from, to, 1, 10);

            items.Should().HaveCount(1);
            totalCount.Should().Be(1);
        }

        [Fact]
        public async Task GetPagedByDateRangeAsync_OutOfRange_ShouldReturnEmpty()
        {
            await _repository.AddAsync(new Category("Électronique", 19m));
            await _repository.SaveChangesAsync();

            var from = DateTime.UtcNow.AddDays(-10);
            var to = DateTime.UtcNow.AddDays(-9);

            var (items, totalCount) = await _repository.GetPagedByDateRangeAsync(from, to, 1, 10);

            items.Should().BeEmpty();
            totalCount.Should().Be(0);
        }
    }
}