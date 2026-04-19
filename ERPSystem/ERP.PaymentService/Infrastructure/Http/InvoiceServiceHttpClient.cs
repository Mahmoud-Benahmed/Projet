using ERP.PaymentService.Application.Interfaces;
using ERP.PaymentService.Domain.LocalCache;
using System.Text.Json;

namespace ERP.PaymentService.Infrastructure.Http
{
    public class InvoiceServiceHttpClient : IInvoiceServiceHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<InvoiceServiceHttpClient> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public InvoiceServiceHttpClient(
            ILogger<InvoiceServiceHttpClient> logger,
            HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task<Invoice?> GetInvoiceAsync(Guid invoiceId)
        {
            _logger.LogInformation("\n\nFetching invoice {InvoiceId} from InvoiceService\n\n", invoiceId);

            try
            {
                var response = await _httpClient.GetAsync($"api/invoices/{invoiceId}");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "\n\nInvoiceService returned {StatusCode} for invoice {InvoiceId}\n\n",
                        response.StatusCode, invoiceId);
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var invoice = JsonSerializer.Deserialize<Invoice>(content, JsonOptions);

                _logger.LogInformation(
                    "\n\nSuccessfully fetched invoice {InvoiceId} from InvoiceService\n\n",
                    invoiceId);

                return invoice;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "\n\nFailed to fetch invoice {InvoiceId} from InvoiceService\n\n", invoiceId);
                return null;
            }
        }
    }
}
