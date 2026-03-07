using ERP.ArticleService.Application.DTOs;
using ERP.ArticleService.Application.Interfaces;
using ERP.ArticleService.Domain;
using Microsoft.AspNetCore.Mvc;

namespace ERP.ArticleService.API.Controllers
{
    [ApiController]
    public class ArticleController : ControllerBase
    {
        private readonly IArticleService _articleService;

        public ArticleController(IArticleService articleService)
        {
            _articleService = articleService;
        }

        // =========================
        // GET ALL
        // =========================
        [HttpGet(ApiRoutes.Articles.GetAll)]
        public async Task<ActionResult<List<Article>>> GetAll()
        {
            var articles = await _articleService.GetAllAsync();
            return Ok(articles);
        }

        // =========================
        // GET BY ID
        // =========================
        [HttpGet(ApiRoutes.Articles.GetById)]
        public async Task<ActionResult<Article>> GetById([FromRoute] Guid id)
        {
            try
            {
                var article = await _articleService.GetByIdAsync(id);
                return Ok(article);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        // =========================
        // GET BY CODE
        // =========================
        [HttpGet(ApiRoutes.Articles.GetByCode)]
        public async Task<ActionResult<Article>> GetByCode([FromRoute] string code)
        {
            try
            {
                var article = await _articleService.GetByCodeAsync(code);
                return Ok(article);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        // =========================
        // GET PAGED BY CATEGORY
        // =========================
        [HttpGet(ApiRoutes.Articles.GetPagedByCategory)]
        public async Task<ActionResult> GetPagedByCategory(
            [FromQuery] Guid categoryId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var result = await _articleService.GetPagedByCategoryIdAsync(
                    categoryId, pageNumber, pageSize);
                return Ok(new { result.Items, result.TotalCount });
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet(ApiRoutes.Articles.Stats)]
        [ProducesResponseType(typeof(ArticleStatsDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStats()
        {
            var result = await _articleService.GetStatsAsync();
            return Ok(result);
        }

        // =========================
        // GET PAGED BY STATUS
        // =========================
        [HttpGet(ApiRoutes.Articles.GetPagedByStatus)]
        public async Task<ActionResult> GetPagedByStatus(
            [FromQuery] bool isActive,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var result = await _articleService.GetPagedByStatusAsync(
                    isActive, pageNumber, pageSize);
                return Ok(new { result.Items, result.TotalCount });
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // =========================
        // GET PAGED BY LIBELLE
        // =========================
        [HttpGet(ApiRoutes.Articles.GetPagedByLibelle)]
        public async Task<ActionResult> GetPagedByLibelle(
            [FromQuery] string libelleFilter,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var result = await _articleService.GetPagedByLibelleAsync(
                    libelleFilter, pageNumber, pageSize);
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
        [HttpPost(ApiRoutes.Articles.Create)]
        public async Task<ActionResult<Article>> Create([FromBody] CreateArticleRequestDto request)
        {
            try
            {
                var article = await _articleService.CreateAsync(request);
                return CreatedAtAction(
                    nameof(GetById),
                    new { id = article.Id },
                    article);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // =========================
        // UPDATE
        // =========================
        [HttpPut(ApiRoutes.Articles.Update)]
        public async Task<ActionResult<Article>> Update(
            [FromRoute] Guid id,
            [FromBody] UpdateArticleRequestDto request)
        {
            try
            {
                var article = await _articleService.UpdateAsync(id, request);
                return Ok(article);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // =========================
        // ACTIVATE
        // =========================
        [HttpPatch(ApiRoutes.Articles.Activate)]
        public async Task<ActionResult> Activate([FromRoute] Guid id)
        {
            try
            {
                await _articleService.ActivateAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        // =========================
        // DEACTIVATE
        // =========================
        [HttpPatch(ApiRoutes.Articles.Deactivate)]
        public async Task<ActionResult> Deactivate([FromRoute] Guid id)
        {
            try
            {
                await _articleService.DeactivateAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        // =========================
        // DELETE
        // =========================
        [HttpDelete(ApiRoutes.Articles.Delete)]
        public async Task<ActionResult> Delete([FromRoute] Guid id)
        {
            try
            {
                await _articleService.DeleteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}