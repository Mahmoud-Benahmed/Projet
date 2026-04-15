using ERP.StockService.Infrastructure.Persistence.Repositories;
using ERP.StockService.Properties;
using Microsoft.AspNetCore.Mvc;

namespace ERP.StockService.Controllers
{
    [ApiController]
    [Route(ApiRoutes.Base)]
    public class StockController : ControllerBase
    {
        private readonly IJournalStockRepository _journalStockRepository;
        private readonly ILogger<StockController> _logger;

        public StockController(
            IJournalStockRepository journalStockRepository,
            ILogger<StockController> logger)
        {
            _journalStockRepository = journalStockRepository ??
                throw new ArgumentNullException(nameof(journalStockRepository));
            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet("quantity/{articleId:guid}")]
        public async Task<IActionResult> GetCurrentStock(Guid articleId)
        {
            try
            {
                if (articleId == Guid.Empty)
                {
                    return BadRequest(new { error = "Invalid article ID" });
                }

                // GetCurrentStockAsync returns decimal (non-nullable)
                var stock = await _journalStockRepository.GetCurrentStockAsync(articleId);
                var currentStock = stock;

                return Ok(new
                {
                    articleId = articleId,
                    currentStock = currentStock
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting current stock for article {ArticleId}", articleId);
                return StatusCode(500, new { error = "Failed to retrieve stock", details = ex.Message });
            }
        }


        [HttpGet("articles")]
        [ProducesResponseType(typeof(StockStatusResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStockStatus()
        {
            var stockStatus = await _journalStockRepository.GetArticlesWithStockAsync();
            return Ok(stockStatus);
        }
    }
}