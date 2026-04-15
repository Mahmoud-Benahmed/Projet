namespace ERP.InvoiceService.Infrastructure.Messaging;

public interface IStockServiceHttpClient
{
    Task<StockStatusResponse> GetStockStatusAsync();
}
