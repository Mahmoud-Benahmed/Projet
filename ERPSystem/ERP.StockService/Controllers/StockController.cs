using ERP.StockService.Application.DTOs;
using ERP.StockService.Application.Interfaces;
using ERP.StockService.Infrastructure.Persistence.Repositories;
using ERP.StockService.Properties;
using Microsoft.AspNetCore.Mvc;

namespace ERP.StockService.Controllers
{
    [ApiController]
    [Route(ApiRoutes.Base)]
    public class StockController : ControllerBase
    {
        private readonly IArticleCacheService _articleCacheService;
        private readonly IClientCacheService _clientCacheService;
        private readonly IFournisseurCacheService _fournisseurCacheService;
        private readonly IJournalStockRepository _journalStockRepository;
        private readonly ILogger<StockController> _logger;

        public StockController(
            IJournalStockRepository journalStockRepository,
            ILogger<StockController> logger,
            IArticleCacheService articleCacheService,
            IClientCacheService clientCacheService,
            IFournisseurCacheService fournisseurCacheService)
        {
            _journalStockRepository = journalStockRepository ??
                throw new ArgumentNullException(nameof(journalStockRepository));
            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));
            _articleCacheService = articleCacheService ??
                throw new ArgumentNullException(nameof(articleCacheService));
            _clientCacheService = clientCacheService ??
                throw new ArgumentNullException(nameof(clientCacheService));
            _fournisseurCacheService = fournisseurCacheService ??
                throw new ArgumentNullException(nameof(fournisseurCacheService));
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
                decimal stock = await _journalStockRepository.GetCurrentStockAsync(articleId);
                decimal currentStock = stock;

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
            Dictionary<string, List<StockItem>> stockStatus = await _journalStockRepository.GetArticlesWithStockAsync();
            return Ok(stockStatus);
        }



        // ════════════════════════════════════════════════════════════════════════════
        // ARTICLES CACHE
        // ════════════════════════════════════════════════════════════════════════════
        [HttpGet(ApiRoutes.Cache.Articles.GetById)]
        public async Task<ActionResult<ArticleResponseDto?>> GetArticleCacheById([FromRoute] Guid id)
        {
            return Ok(await _articleCacheService.GetByIdAsync(id));
        }

        // invoices/cache/articles

        [HttpGet(ApiRoutes.Cache.Articles.GetPaged)]
        public async Task<ActionResult<PagedResultDto<ArticleResponseDto>>> GetArticleCachePagedAsync([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
        {
            return Ok(await _articleCacheService.GetPagedAsync(pageNumber, pageSize, search));
        }



        // ════════════════════════════════════════════════════════════════════════════
        // CLIENTS CACHE
        // ════════════════════════════════════════════════════════════════════════════
        [HttpGet(ApiRoutes.Cache.Clients.GetById)]
        public async Task<ActionResult<ClientResponseDto?>> GetClientCacheById([FromRoute] Guid id)
        {
            return Ok(await _clientCacheService.GetByIdAsync(id));
        }

        // invoices/cache/clients

        [HttpGet(ApiRoutes.Cache.Clients.GetPaged)]
        public async Task<ActionResult<PagedResultDto<ClientResponseDto>>> GetClientCachePagedAsync(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null)
        {
            return Ok(await _clientCacheService.GetPagedAsync(pageNumber, pageSize, search));
        }



        // ════════════════════════════════════════════════════════════════════════════
        // FOURNISSEUR CACHE
        // ════════════════════════════════════════════════════════════════════════════

        [HttpGet(ApiRoutes.Cache.Fournisseurs.GetById)]
        public async Task<ActionResult<FournisseurResponseDto?>> GetFournisseurCacheById([FromRoute] Guid id)
        {
            return Ok(await _fournisseurCacheService.GetByIdAsync(id));
        }

        // invoices/cache/fournisseurs

        [HttpGet(ApiRoutes.Cache.Fournisseurs.GetPaged)]
        public async Task<ActionResult<PagedResultDto<FournisseurResponseDto>>> GetFournisseurCachePagedAsync([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null)
        {
            return Ok(await _fournisseurCacheService.GetPagedAsync(pageNumber, pageSize, search));
        }
    }
}