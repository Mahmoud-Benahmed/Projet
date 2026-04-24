using System.Text.Json;

namespace ERP.InvoiceService.Infrastructure.Messaging;

public class StockServiceHttpClient : IStockServiceHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<StockServiceHttpClient>? _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public StockServiceHttpClient(HttpClient httpClient, ILogger<StockServiceHttpClient>? logger = null)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Gets complete stock status (in stock and out of stock articles)
    /// </summary>
    public async Task<StockStatusResponse> GetStockStatusAsync()
    {
        try
        {
            HttpResponseMessage response;

            response = await _httpClient.GetAsync("stock/articles");
            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync();
            StockStatusResponse? result = JsonSerializer.Deserialize<StockStatusResponse>(json, _jsonOptions);

            return result ?? new StockStatusResponse();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get stock status");
            throw;
        }
    }
}