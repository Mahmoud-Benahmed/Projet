using ERP.ArticleService.Domain;
using FluentAssertions;

namespace ERP.ArticleService.Tests.Unit.Domain
{
    public class ArticleTests
    {
        private readonly Category _category = new("Électronique", 19m);

        // =========================
        // CONSTRUCTOR
        // =========================
        [Fact]
        public void Constructor_ValidInputs_ShouldCreateArticle()
        {
            var article = new Article("ART-2026-000001", "Écran 27 pouces", 1299.99m, _category, "1234567890128", 19m);

            article.Id.Should().NotBeEmpty();
            article.CodeRef.Should().Be("ART-2026-000001");
            article.Libelle.Should().Be("Écran 27 pouces");
            article.Prix.Should().Be(1299.99m);
            article.CategoryId.Should().Be(_category.Id);
            article.BarCode.Should().Be("1234567890128");
            article.TVA.Should().Be(19m);
            article.IsDeleted.Should().BeFalse();
            article.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        }

        [Fact]
        public void Constructor_NullTVA_ShouldInheritFromCategory()
        {
            var article = new Article("ART-2026-000001", "Écran 27 pouces", 1299.99m, _category, "1234567890128", null);
            article.TVA.Should().Be(_category.TVA);
        }

        [Fact]
        public void Constructor_ShouldTrimLibelle()
        {
            var article = new Article("ART-2026-000001", "  Écran 27 pouces  ", 1299.99m, _category, "1234567890128", 19m);
            article.Libelle.Should().Be("Écran 27 pouces");
        }

        [Fact]
        public void Constructor_ShouldRoundPrixToTwoDecimals()
        {
            var article = new Article("ART-2026-000001", "Écran", 1299.999m, _category, "1234567890128", 19m);
            article.Prix.Should().Be(1300.00m);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Constructor_EmptyLibelle_ShouldThrowArgumentException(string libelle)
        {
            Action act = () => new Article("ART-2026-000001", libelle, 100m, _category, "1234567890128", 19m);
            act.Should().Throw<ArgumentException>()
               .WithMessage("*Libelle is required*");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Constructor_EmptyCode_ShouldThrowArgumentException(string code)
        {
            Action act = () => new Article(code, "Écran", 100m, _category, "1234567890128", 19m);
            act.Should().Throw<ArgumentException>()
               .WithMessage("*Code is required*");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-999)]
        public void Constructor_InvalidPrix_ShouldThrowArgumentException(decimal prix)
        {
            Action act = () => new Article("ART-2026-000001", "Écran", prix, _category, "1234567890128", 19m);
            act.Should().Throw<ArgumentException>()
               .WithMessage("*Prix must be positive*");
        }

        [Fact]
        public void Constructor_NullCategory_ShouldThrowArgumentException()
        {
            Action act = () => new Article("ART-2026-000001", "Écran", 100m, null, "1234567890128", 19m);
            act.Should().Throw<ArgumentException>()
               .WithMessage("*Category is required*");
        }

        // =========================
        // UPDATE
        // =========================
        [Fact]
        public void Update_ValidInputs_ShouldUpdateArticle()
        {
            var article = new Article("ART-2026-000001", "Écran 27 pouces", 1299.99m, _category, "1234567890128", 19m);
            var newCategory = new Category("Informatique", 15m);
            var before = DateTime.Now;

            article.Update("Écran 32 pouces", 1599.99m, newCategory, "9876543210987", 15m);

            article.Libelle.Should().Be("Écran 32 pouces");
            article.Prix.Should().Be(1599.99m);
            article.CategoryId.Should().Be(newCategory.Id);
            article.BarCode.Should().Be("9876543210987");
            article.TVA.Should().Be(15m);
            article.UpdatedAt.Should().BeOnOrAfter(before);
        }

        [Fact]
        public void Update_NullTVA_ShouldInheritFromCategory()
        {
            var article = new Article("ART-2026-000001", "Écran", 1299.99m, _category, "1234567890128", 19m);
            var newCategory = new Category("Informatique", 15m);

            article.Update("Écran 32 pouces", 1599.99m, newCategory, "9876543210987", null);

            article.TVA.Should().Be(newCategory.TVA);
        }

        [Fact]
        public void Update_NoChanges_ShouldNotUpdateTimestamp()
        {
            var article = new Article("ART-2026-000001", "Écran 27 pouces", 1299.99m, _category, "1234567890128", 19m);
            var before = article.UpdatedAt;

            article.Update("Écran 27 pouces", 1299.99m, _category, "1234567890128", 19m);

            article.UpdatedAt.Should().Be(before);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Update_EmptyLibelle_ShouldThrowArgumentException(string libelle)
        {
            var article = new Article("ART-2026-000001", "Écran", 100m, _category, "1234567890128", 19m);
            Action act = () => article.Update(libelle, 100m, _category, "1234567890128", 19m);
            act.Should().Throw<ArgumentException>()
               .WithMessage("*Libelle is required*");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Update_InvalidPrix_ShouldThrowArgumentException(decimal prix)
        {
            var article = new Article("ART-2026-000001", "Écran", 100m, _category, "1234567890128", 19m);
            Action act = () => article.Update("Écran", prix, _category, "1234567890128", 19m);
            act.Should().Throw<ArgumentException>()
               .WithMessage("*Prix must be positive*");
        }
    }
}