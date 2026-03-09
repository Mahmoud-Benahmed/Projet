using ERP.ArticleService.Application.DTOs;
using ERP.ArticleService.Application.Interfaces;
using ERP.ArticleService.Domain;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ERP.ArticleService.API.Controllers;

namespace ERP.ArticleService.Tests.Integration.Controllers
{
    public class CategoryControllerTests
    {
        private readonly Mock<ICategoryService> _serviceMock = new();
        private readonly CategoryController _controller;

        public CategoryControllerTests()
        {
            _controller = new CategoryController(_serviceMock.Object);
        }

        // =========================
        // GET ALL
        // =========================
        [Fact]
        public async Task GetAll_ShouldReturn200WithCategories()
        {
            var categories = new List<Category>
            {
                new("Électronique", 19m),
                new("Informatique", 15m)
            };
            _serviceMock.Setup(s => s.GetAllAsync()).ReturnsAsync(categories);

            var result = await _controller.GetAll();

            var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var body = ok.Value.Should().BeAssignableTo<List<Category>>().Subject;
            body.Should().HaveCount(2);
        }

        // =========================
        // GET BY ID
        // =========================
        [Fact]
        public async Task GetById_ExistingId_ShouldReturn200()
        {
            var category = new Category("Électronique", 19m);
            _serviceMock.Setup(s => s.GetByIdAsync(category.Id)).ReturnsAsync(category);

            var result = await _controller.GetById(category.Id);

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
        // GET BY NAME
        // =========================
        [Fact]
        public async Task GetByName_ExistingName_ShouldReturn200()
        {
            var category = new Category("Électronique", 19m);
            _serviceMock.Setup(s => s.GetByNameAsync("Électronique")).ReturnsAsync(category);

            var result = await _controller.GetByName("Électronique");

            result.Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetByName_NonExistingName_ShouldReturn404()
        {
            _serviceMock.Setup(s => s.GetByNameAsync(It.IsAny<string>()))
                        .ThrowsAsync(new KeyNotFoundException("Not found"));

            var result = await _controller.GetByName("Unknown");

            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        // =========================
        // CREATE
        // =========================
        [Fact]
        public async Task Create_ValidName_ShouldReturn201()
        {
            var category = new Category("Électronique", 19m);
            _serviceMock.Setup(s => s.CreateAsync("Électronique", 19m)).ReturnsAsync(category);


            var result = await _controller.Create(new CategoryRequestDto (Name: "Électronique", TVA: 19m ));

            result.Result.Should().BeOfType<CreatedAtActionResult>();
        }

        [Fact]
        public async Task Create_DuplicateName_ShouldReturn409()
        {
            _serviceMock.Setup(s => s.CreateAsync(It.IsAny<string>(), It.IsAny<decimal>()))
                        .ThrowsAsync(new InvalidOperationException("Already exists"));

            var result = await _controller.Create(new CategoryRequestDto(Name: "Électronique", TVA: 19m));

            result.Result.Should().BeOfType<ConflictObjectResult>();
        }

        // =========================
        // UPDATE NAME
        // =========================
        [Fact]
        public async Task UpdateAsync_ValidInputs_ShouldReturn200()
        {
            var category = new Category("Informatique", 15m);
            _serviceMock.Setup(s => s.UpdateAsync(category.Id, "Informatique", 15m))
                        .ReturnsAsync(category);

            var result = await _controller.Update(category.Id, new CategoryRequestDto(
                Name: "Informatique",
                TVA: 15m
            ));

            result.Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task UpdateAsync_NotFound_ShouldReturn404()
        {
            _serviceMock.Setup(s => s.UpdateAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<decimal>()))
                        .ThrowsAsync(new KeyNotFoundException("Not found"));

            var result = await _controller.Update(Guid.NewGuid(), new CategoryRequestDto
            (
                Name: "Informatique",
                TVA: 15m
            ));

            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }
        // =========================
        // DELETE
        // =========================
        [Fact]
        public async Task Delete_ExistingCategory_ShouldReturn204()
        {
            var id = Guid.NewGuid();
            _serviceMock.Setup(s => s.DeleteAsync(id)).Returns(Task.CompletedTask);

            var result = await _controller.Delete(id);

            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task Delete_NonExistingCategory_ShouldReturn404()
        {
            _serviceMock.Setup(s => s.DeleteAsync(It.IsAny<Guid>()))
                        .ThrowsAsync(new KeyNotFoundException("Not found"));

            var result = await _controller.Delete(Guid.NewGuid());

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        // =========================
        // TVA FILTERING
        // =========================
        [Fact]
        public async Task GetBelowTVA_ValidTVA_ShouldReturn200()
        {
            var categories = new List<Category> { new("Alimentaire", 5m) };
            _serviceMock.Setup(s => s.GetBelowTVAAsync(10m)).ReturnsAsync(categories);

            var result = await _controller.GetBelowTVA(10m);

            result.Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetBelowTVA_InvalidTVA_ShouldReturn400()
        {
            _serviceMock.Setup(s => s.GetBelowTVAAsync(It.IsAny<decimal>()))
                        .ThrowsAsync(new ArgumentException("TVA must be greater than zero."));

            var result = await _controller.GetBelowTVA(0m);

            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task GetHigherThanTVA_ValidTVA_ShouldReturn200()
        {
            var categories = new List<Category> { new("Luxe", 20m) };
            _serviceMock.Setup(s => s.GetHigherThanTVAAsync(15m)).ReturnsAsync(categories);

            var result = await _controller.GetHigherThanTVA(15m);

            result.Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetBetweenTVA_ValidRange_ShouldReturn200()
        {
            var categories = new List<Category> { new("Électronique", 19m) };
            _serviceMock.Setup(s => s.GetBetweenTVAAsync(10m, 20m)).ReturnsAsync(categories);

            var result = await _controller.GetBetweenTVA(10m, 20m);

            result.Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetBetweenTVA_InvalidRange_ShouldReturn400()
        {
            _serviceMock.Setup(s => s.GetBetweenTVAAsync(It.IsAny<decimal>(), It.IsAny<decimal>()))
                        .ThrowsAsync(new ArgumentException("'min' TVA must be less than or equal to 'max'"));

            var result = await _controller.GetBetweenTVA(20m, 10m);

            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }
    }
}