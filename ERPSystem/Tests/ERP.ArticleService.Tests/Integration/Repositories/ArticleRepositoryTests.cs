using ERP.ArticleService.Domain;
using ERP.ArticleService.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ERP.ArticleService.Tests.Integration.Repositories
{
    public class ArticleRepositoryTests : IDisposable
    {
        private readonly ArticleDbContext _context;
        private readonly ArticleRepository _repository;
        private readonly Category _category;

        public ArticleRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<ArticleDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ArticleDbContext(options);
            _repository = new ArticleRepository(_context);

            // Seed a category for all tests
            _category = new Category("Électronique", 19m);
            _context.Categories.Add(_category);
            _context.SaveChanges();
        }

        public void Dispose() => _context.Dispose();

        private Article CreateArticle(
            string code = "ART-2026-000001",
            string libelle = "Écran 27 pouces",
            decimal prix = 1299.99m,
            string barCode = "1234567890128",
            decimal tva = 19m)
            => new Article(code, libelle, prix, _category, barCode, tva);

        // =========================
        // ADD & GET BY ID
        // =========================
        [Fact]
        public async Task AddAsync_ThenGetByIdAsync_ShouldReturnArticle()
        {
            var article = CreateArticle();
            await _repository.AddAsync(article);
            await _repository.SaveChangesAsync();

            var result = await _repository.GetByIdAsync(article.Id);

            result.Should().NotBeNull();
            result!.Libelle.Should().Be("Écran 27 pouces");
            result.Prix.Should().Be(1299.99m);
            result.BarCode.Should().Be("1234567890128");
            result.TVA.Should().Be(19m);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ShouldReturnNull()
        {
            var result = await _repository.GetByIdAsync(Guid.NewGuid());
            result.Should().BeNull();
        }

        // =========================
        // GET BY CODE
        // =========================
        [Fact]
        public async Task GetByCodeAsync_ExistingCode_ShouldReturnArticle()
        {
            var article = CreateArticle(code: "ART-2026-000001");
            await _repository.AddAsync(article);
            await _repository.SaveChangesAsync();

            var result = await _repository.GetByCodeAsync("ART-2026-000001");

            result.Should().NotBeNull();
            result!.CodeRef.Should().Be("ART-2026-000001");
        }

        [Fact]
        public async Task GetByCodeAsync_NonExistingCode_ShouldReturnNull()
        {
            var result = await _repository.GetByCodeAsync("UNKNOWN");
            result.Should().BeNull();
        }

        // =========================
        // GET ALL
        // =========================
        [Fact]
        public async Task GetAllAsync_ShouldReturnAllArticles()
        {
            await _repository.AddAsync(CreateArticle("ART-2026-000001", "Écran", 100m, "1234567890128"));
            await _repository.AddAsync(CreateArticle("ART-2026-000002", "Clavier", 50m, "9876543210987"));
            await _repository.AddAsync(CreateArticle("ART-2026-000003", "Souris", 30m, "1122334455667"));
            await _repository.SaveChangesAsync();

            var result = await _repository.GetAllAsync();

            result.Should().HaveCount(3);
        }

        [Fact]
        public async Task GetAllAsync_EmptyDatabase_ShouldReturnEmptyList()
        {
            var result = await _repository.GetAllAsync();
            result.Should().BeEmpty();
        }

        // =========================
        // REMOVE
        // =========================
        [Fact]
        public async Task Remove_ExistingArticle_ShouldDeleteFromDatabase()
        {
            var article = CreateArticle();
            await _repository.AddAsync(article);
            await _repository.SaveChangesAsync();

            _repository.Remove(article);
            await _repository.SaveChangesAsync();

            var result = await _repository.GetByIdAsync(article.Id);
            result.Should().BeNull();
        }

        // =========================
        // PAGED BY CATEGORY
        // =========================
        [Fact]
        public async Task GetPagedByCategoryIdAsync_ShouldReturnOnlyMatchingCategory()
        {
            var otherCategory = new Category("Informatique", 15m);
            _context.Categories.Add(otherCategory);
            await _context.SaveChangesAsync();

            await _repository.AddAsync(CreateArticle("ART-2026-000001", "Écran", 100m, "1234567890128"));
            await _repository.AddAsync(CreateArticle("ART-2026-000002", "Clavier", 50m, "9876543210987"));
            await _repository.AddAsync(new Article("ART-2026-000003", "Laptop", 5000m, otherCategory, "1122334455667", 15m));
            await _repository.SaveChangesAsync();

            var (items, totalCount) = await _repository.GetPagedByCategoryIdAsync(_category.Id, 1, 10);

            items.Should().HaveCount(2);
            totalCount.Should().Be(2);
            items.All(a => a.CategoryId == _category.Id).Should().BeTrue();
        }

        [Fact]
        public async Task GetPagedByCategoryIdAsync_ShouldReturnCorrectPage()
        {
            for (int i = 1; i <= 15; i++)
                await _repository.AddAsync(CreateArticle($"ART-2026-{i:D6}", $"Article {i}", 100m, $"{i:D13}"));
            await _repository.SaveChangesAsync();

            var (items, totalCount) = await _repository.GetPagedByCategoryIdAsync(_category.Id, 1, 10);

            items.Should().HaveCount(10);
            totalCount.Should().Be(15);
        }

        // =========================
        // PAGED BY STATUS
        // =========================
        [Fact]
        public async Task GetPagedByStatusAsync_ActiveOnly_ShouldReturnActiveArticles()
        {
            var active = CreateArticle("ART-2026-000001", "Écran", 100m, "1234567890128");
            var inactive = CreateArticle("ART-2026-000002", "Clavier", 50m, "9876543210987");
            inactive.Deactivate();

            await _repository.AddAsync(active);
            await _repository.AddAsync(inactive);
            await _repository.SaveChangesAsync();

            var (items, totalCount) = await _repository.GetPagedByStatusAsync(true, 1, 10);

            items.Should().HaveCount(1);
            totalCount.Should().Be(1);
            items.All(a => a.IsActive).Should().BeTrue();
        }

        [Fact]
        public async Task GetPagedByStatusAsync_InactiveOnly_ShouldReturnInactiveArticles()
        {
            var active = CreateArticle("ART-2026-000001", "Écran", 100m, "1234567890128");
            var inactive = CreateArticle("ART-2026-000002", "Clavier", 50m, "9876543210987");
            inactive.Deactivate();

            await _repository.AddAsync(active);
            await _repository.AddAsync(inactive);
            await _repository.SaveChangesAsync();

            var (items, totalCount) = await _repository.GetPagedByStatusAsync(false, 1, 10);

            items.Should().HaveCount(1);
            totalCount.Should().Be(1);
            items.All(a => !a.IsActive).Should().BeTrue();
        }

        // =========================
        // PAGED BY LIBELLE
        // =========================
        [Fact]
        public async Task GetPagedByLibelleAsync_ShouldFilterByLibelle()
        {
            await _repository.AddAsync(CreateArticle("ART-2026-000001", "Écran 27 pouces", 100m, "1234567890128"));
            await _repository.AddAsync(CreateArticle("ART-2026-000002", "Écran 32 pouces", 200m, "9876543210987"));
            await _repository.AddAsync(CreateArticle("ART-2026-000003", "Clavier mécanique", 50m, "1122334455667"));
            await _repository.SaveChangesAsync();

            var (items, totalCount) = await _repository.GetPagedByLibelleAsync("Écran", 1, 10);

            items.Should().HaveCount(2);
            totalCount.Should().Be(2);
        }

        [Fact]
        public async Task GetPagedByLibelleAsync_NoMatch_ShouldReturnEmpty()
        {
            await _repository.AddAsync(CreateArticle("ART-2026-000001", "Écran", 100m, "1234567890128"));
            await _repository.SaveChangesAsync();

            var (items, totalCount) = await _repository.GetPagedByLibelleAsync("Laptop", 1, 10);

            items.Should().BeEmpty();
            totalCount.Should().Be(0);
        }

        [Fact]
        public async Task GetPagedByLibelleAsync_ShouldReturnCorrectPage()
        {
            for (int i = 1; i <= 15; i++)
                await _repository.AddAsync(CreateArticle($"ART-2026-{i:D6}", $"Écran {i}", 100m, $"{i:D13}"));
            await _repository.SaveChangesAsync();

            var (items, totalCount) = await _repository.GetPagedByLibelleAsync("Écran", 2, 10);

            items.Should().HaveCount(5);
            totalCount.Should().Be(15);
        }
    }
}