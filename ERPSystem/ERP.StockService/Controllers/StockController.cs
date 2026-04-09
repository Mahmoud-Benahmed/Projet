using ERP.StockService.Properties;
using Microsoft.AspNetCore.Mvc;

namespace ERP.StockService.Controllers
{
    public class StockController: ControllerBase
    {
        private readonly IJournalStockRepository _journalStockRepository;

        // StockController or a dedicated endpoint
        [HttpGet($"{ApiRoutes.Base}/quantity/{{articleId:guid}}")]
        public async Task<IActionResult> GetCurrentStock(Guid articleId)
        {
            var stock = await _journalStockRepository.GetCurrentStockAsync(articleId);
            return Ok(new { articleId, currentStock = stock });
        }
    }
}
