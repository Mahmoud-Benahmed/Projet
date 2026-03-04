using ERP.ArticleService.API;
using ERP.ArticleService.Application.Interfaces;
using ERP.ArticleService.Domain;
using Microsoft.AspNetCore.Mvc;

namespace ERP.ArticleService.API.Controllers
{
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoryController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        // =========================
        // GET ALL
        // =========================
        [HttpGet(ApiRoutes.Categories.GetAll)]
        public async Task<ActionResult<List<Category>>> GetAll()
        {
            var categories = await _categoryService.GetAllAsync();
            return Ok(categories);
        }

        // =========================
        // GET BY ID
        // =========================
        [HttpGet(ApiRoutes.Categories.GetById)]
        public async Task<ActionResult<Category>> GetById([FromRoute] Guid id)
        {
            try
            {
                var category = await _categoryService.GetByIdAsync(id);
                return Ok(category);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        // =========================
        // GET BY NAME
        // =========================
        [HttpGet(ApiRoutes.Categories.GetByName)]
        public async Task<ActionResult<Category>> GetByName([FromRoute] string name)
        {
            try
            {
                var category = await _categoryService.GetByNameAsync(name);
                return Ok(category);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        // =========================
        // GET PAGED
        // =========================
        [HttpGet(ApiRoutes.Categories.GetPaged)]
        public async Task<ActionResult> GetPaged(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var result = await _categoryService.GetPagedAsync(pageNumber, pageSize);
                return Ok(new { result.Items, result.TotalCount });
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // =========================
        // GET PAGED BY NAME
        // =========================
        [HttpGet(ApiRoutes.Categories.GetPagedByName)]
        public async Task<ActionResult> GetPagedByName(
            [FromQuery] string nameFilter,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var result = await _categoryService.GetPagedByNameAsync(
                    nameFilter, pageNumber, pageSize);
                return Ok(new { result.Items, result.TotalCount });
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // =========================
        // GET PAGED BY DATE RANGE
        // =========================
        [HttpGet(ApiRoutes.Categories.GetPagedByDateRange)]
        public async Task<ActionResult> GetPagedByDateRange(
            [FromQuery] DateTime from,
            [FromQuery] DateTime to,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var result = await _categoryService.GetPagedByDateRangeAsync(
                    from, to, pageNumber, pageSize);
                return Ok(new { result.Items, result.TotalCount });
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // =========================
        // CREATE
        // =========================
        [HttpPost(ApiRoutes.Categories.Create)]
        public async Task<ActionResult<Category>> Create([FromBody] string name)
        {
            try
            {
                var category = await _categoryService.CreateAsync(name);
                return CreatedAtAction(
                    nameof(GetById),
                    new { id = category.Id },
                    category);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // =========================
        // UPDATE NAME
        // =========================
        [HttpPut(ApiRoutes.Categories.UpdateName)]
        public async Task<ActionResult<Category>> UpdateName(
            [FromRoute] Guid id,
            [FromBody] string newName)
        {
            try
            {
                var category = await _categoryService.UpdateNameAsync(id, newName);
                return Ok(category);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // =========================
        // DELETE
        // =========================
        [HttpDelete(ApiRoutes.Categories.Delete)]
        public async Task<ActionResult> Delete([FromRoute] Guid id)
        {
            try
            {
                await _categoryService.DeleteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}