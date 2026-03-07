using ERP.ArticleService.Domain;
using FluentAssertions;

namespace ERP.ArticleService.Tests.Unit.Domain
{
    public class CategoryTests
    {
        // =========================
        // CONSTRUCTOR
        // =========================
        [Fact]
        public void Constructor_ValidInputs_ShouldCreateCategory()
        {
            var category = new Category("Électronique", 19.99m);

            category.Id.Should().NotBeEmpty();
            category.Name.Should().Be("Électronique");
            category.TVA.Should().Be(19.99m);
            category.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
            category.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        }

        [Fact]
        public void Constructor_ShouldTrimName()
        {
            var category = new Category("  Électronique  ", 10m);
            category.Name.Should().Be("Électronique");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Constructor_EmptyName_ShouldThrowArgumentException(string name)
        {
            Action act = () => new Category(name, 10m);
            act.Should().Throw<ArgumentException>()
               .WithMessage("*Category name is required*");
        }

        [Fact]
        public void Constructor_NegativeTVA_ShouldThrowArgumentException()
        {
            Action act = () => new Category("Électronique", -1m);
            act.Should().Throw<ArgumentException>()
               .WithMessage("*TVA cannot be below 0*");
        }

        [Fact]
        public void Constructor_ZeroTVA_ShouldBeAllowed()
        {
            var category = new Category("Électronique", 0m);
            category.TVA.Should().Be(0m);
        }

        // =========================
        // UPDATE
        // =========================
        [Fact]
        public void Update_ValidInputs_ShouldUpdateNameAndTVA()
        {
            var category = new Category("Électronique", 10m);
            var before = category.UpdatedAt;

            category.Update("Informatique", 20m);

            category.Name.Should().Be("Informatique");
            category.TVA.Should().Be(20m);
            category.UpdatedAt.Should().BeOnOrAfter(before);
        }

        [Fact]
        public void Update_SameName_ShouldNotUpdate()
        {
            var category = new Category("Électronique", 10m);
            var before = category.UpdatedAt;

            category.Update("Électronique", 15m);

            // Same name → no update
            category.UpdatedAt.Should().Be(before);
        }

        [Fact]
        public void Update_NegativeTVA_ShouldThrowArgumentException()
        {
            var category = new Category("Électronique", 10m);
            Action act = () => category.Update("Électronique", -5m);
            act.Should().Throw<ArgumentException>()
               .WithMessage("*TVA cannot be below 0*");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Update_EmptyName_ShouldThrowArgumentException(string name)
        {
            var category = new Category("Électronique", 10m);
            Action act = () => category.Update(name, 10m);
            act.Should().Throw<ArgumentException>()
               .WithMessage("*Category name is required*");
        }
    }
}