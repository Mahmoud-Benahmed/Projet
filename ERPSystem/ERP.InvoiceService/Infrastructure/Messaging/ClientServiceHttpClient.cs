using ERP.InvoiceService.Application.DTOs;
using System.Net;
using System.Net.Http.Json;

namespace ERP.InvoiceService.Infrastructure.Messaging;

public class ClientServiceHttpClient : IClientServiceHttpClient
{
    private readonly HttpClient _httpClient;

    public ClientServiceHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ClientResponseDto> GetByIdAsync(Guid clientId)
    {
        var response = await _httpClient.GetAsync($"/clients/{clientId}");

        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new KeyNotFoundException($"Client with ID {clientId} not found.");

        response.EnsureSuccessStatusCode();

        // Deserialize JSON to DTO
        var client = await response.Content.ReadFromJsonAsync<ClientResponseDto>();
        if (client == null)
            throw new InvalidOperationException($"Failed to deserialize client {clientId}");

        return client;
    }
}