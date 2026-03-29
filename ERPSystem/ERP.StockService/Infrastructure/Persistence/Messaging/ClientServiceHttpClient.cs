using ERP.StockService.Application.Exceptions;
using System.Net;
namespace ERP.StockService.Infrastructure.Persistence.Messaging;

public class ClientServiceHttpClient : IClientService
{
    private readonly HttpClient _httpClient;

    public ClientServiceHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task ExistsByIdAsync(Guid clientId)
    {
        var response = await _httpClient.GetAsync($"/clients/{clientId}");

        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new ClientNotFoundException(clientId);

        response.EnsureSuccessStatusCode();
    }
}