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
        public async Task<ActionResult> GetAllPagedAsync([FromQuery] int pageNumber = 1,[FromQuery] int pageSize = 10)
        {
            var result = await _articleService.GetAllAsync(pageNumber, pageSize);
            return Ok(new { result.Items, result.TotalCount });
        }


        // =========================
        // GET DELETED
        // =========================
        [HttpGet(ApiRoutes.Articles.GetDeletedRoute)]
        public async Task<ActionResult> GetDeletedAsync([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _articleService.GetPagedDeletedAsync(pageNumber, pageSize);
            return Ok(new { result.Items, result.TotalCount });
        }

        // =========================
        // GET BY ID
        // =========================
        [HttpGet(ApiRoutes.Articles.GetById)]
        public async Task<ActionResult<Article>> GetById([FromRoute] Guid id)
        {

                var article = await _articleService.GetByIdAsync(id);
                return Ok(article);
        }

        // =========================
        // GET BY CODE
        // =========================
        [HttpGet(ApiRoutes.Articles.GetByCode)]
        public async Task<ActionResult<Article>> GetByCode([FromRoute] string code)
        {
            var article = await _articleService.GetByCodeAsync(code);
            return Ok(article);
        }

        // =========================
        // GET PAGED BY CATEGORY
        // =========================
        [HttpGet(ApiRoutes.Articles.GetPagedByCategory)]
        public async Task<ActionResult> GetPagedByCategory([FromQuery] Guid categoryId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
                var result = await _articleService.GetPagedByCategoryIdAsync(categoryId, pageNumber, pageSize);
                return Ok(new { result.Items, result.TotalCount });
        }

        [HttpGet(ApiRoutes.Articles.Stats)]
        [ProducesResponseType(typeof(ArticleStatsDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStats()
        {
            var result = await _articleService.GetStatsAsync();
            return Ok(result);
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
                var result = await _articleService.GetPagedByLibelleAsync(
                    libelleFilter, pageNumber, pageSize);
                return Ok(new { result.Items, result.TotalCount });
        }

        // =========================
        // CREATE
        // =========================
        [HttpPost(ApiRoutes.Articles.Create)]
        public async Task<ActionResult<Article>> Create([FromBody] CreateArticleRequestDto request)
        {
                var article = await _articleService.CreateAsync(request);
                return CreatedAtAction(
                    nameof(GetById),
                    new { id = article.Id },
                    article);
        }

        // =========================
        // UPDATE
        // =========================
        [HttpPut(ApiRoutes.Articles.Update)]
        public async Task<ActionResult<Article>> Update([FromRoute] Guid id, [FromBody] UpdateArticleRequestDto request)
        {
                var article = await _articleService.UpdateAsync(id, request);
                return Ok(article);
        }

        // =========================
        // RESTORE
        // =========================
        [HttpPatch(ApiRoutes.Articles.Restore)]
        public async Task<ActionResult> Restore([FromRoute] Guid id)
        {

                await _articleService.RestoreAsync(id);
                return NoContent();
        }

        // =========================
        // DELETE
        // =========================
        [HttpDelete(ApiRoutes.Articles.Delete)]
        public async Task<ActionResult> Delete([FromRoute] Guid id)
        {
                await _articleService.DeleteAsync(id);
                return NoContent();
        }
    }
}