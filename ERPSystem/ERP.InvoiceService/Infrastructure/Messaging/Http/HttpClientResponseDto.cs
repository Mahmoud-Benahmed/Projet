namespace ERP.InvoiceService.Infrastructure.Messaging;

public class StockStatusResponse
{
    public List<StockItem> IN_STOCK { get; set; } = new();
    public List<StockItem> OUT_STOCK { get; set; } = new();
}

public class StockItem
{
    public Guid ArticleId { get; set; }
    public decimal Quantity { get; set; }
}
