using ERP.InvoiceService.Application.DTOs;
using System.Net;
using System.Text.Json;

namespace ERP.InvoiceService.Infrastructure.Messaging;

public class ClientServiceHttpClient : IClientServiceHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ClientServiceHttpClient>? _logger;

    // Shared options: case-insensitive to tolerate PascalCase / camelCase APIs
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ClientServiceHttpClient(HttpClient httpClient, ILogger<ClientServiceHttpClient>? logger = null)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    private const string baseUrl = "/clients";

    public async Task<ClientResponseDto> GetByIdAsync(Guid clientId)
    {
        try
        {
            _logger?.LogInformation("Fetching client {ClientId} from API", clientId);

            HttpResponseMessage response;

            try
            {
                response = await _httpClient.GetAsync($"{baseUrl}/{clientId}");
            }
            catch (HttpRequestException ex)
            {
                _logger?.LogError(ex, "Network error while fetching client {ClientId}", clientId);
                throw new InvalidOperationException(
                    $"Network error while fetching client {clientId}. Please check if the client service is available.", ex);
            }

            // ── 404 ──────────────────────────────────────────────────────────────────
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger?.LogWarning("Client {ClientId} not found", clientId);
                throw new KeyNotFoundException($"Client with ID {clientId} not found.");
            }

            // ── Any other non-success ────────────────────────────────────────────────
            response.EnsureSuccessStatusCode();

            // ── Deserialize ──────────────────────────────────────────────────────────
            ClientResponseDto? client;

            try
            {
                client = await response.Content
                    .ReadFromJsonAsync<ClientResponseDto>(_jsonOptions);
            }
            catch (JsonException ex)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger?.LogError(ex, "Malformed JSON response for client {ClientId}. Body: {Body}", clientId, body);
                throw new InvalidOperationException(
                    $"Client service returned malformed JSON for client {clientId}. " +
                    $"Body: {body}", ex);
            }

            if (client is null)
            {
                _logger?.LogError("Null body received for client {ClientId}", clientId);
                throw new InvalidOperationException(
                    $"Client service returned a null body for client {clientId}.");
            }

            _logger?.LogInformation("Successfully fetched client {ClientId}: {ClientName}", clientId, client.Name);
            return client;
        }
        catch (Exception ex) when (ex is not KeyNotFoundException && ex is not InvalidOperationException)
        {
            _logger?.LogError(ex, "Unexpected error while fetching client {ClientId}", clientId);
            throw;
        }
    }

    public async Task<PagedResultDto<ClientResponseDto>> GetAllPagedAsync(int pageNumber = 1, int pageSize = 10)
    {
        try
        {
            // Validate parameters
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100; // Max page size limit

            _logger?.LogInformation("Fetching clients page {PageNumber} with size {PageSize}", pageNumber, pageSize);

            var response = await _httpClient.GetAsync($"{baseUrl}?pageNumber={pageNumber}&pageSize={pageSize}");

            if (!response.IsSuccessStatusCode)
            {
                _logger?.LogWarning("Failed to fetch clients. Status code: {StatusCode}", response.StatusCode);

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return new PagedResultDto<ClientResponseDto>(new List<ClientResponseDto>(), 0, pageNumber, pageSize);
                }

                response.EnsureSuccessStatusCode();
            }

            var pagedResult = await response.Content.ReadFromJsonAsync<PagedResultDto<ClientResponseDto>>(_jsonOptions);

            if (pagedResult == null)
            {
                _logger?.LogWarning("Received null response from client service");
                return new PagedResultDto<ClientResponseDto>(new List<ClientResponseDto>(), 0, pageNumber, pageSize);
            }

            _logger?.LogInformation("Successfully fetched {Count} clients out of {TotalCount}",
                pagedResult.Items.Count, pagedResult.TotalCount);

            return pagedResult;
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogError(ex, "Network error while fetching paged clients");
            throw new InvalidOperationException("Network error while fetching clients. Please check if the client service is available.", ex);
        }
        catch (JsonException ex)
        {
            _logger?.LogError(ex, "JSON deserialization error while fetching paged clients");
            throw new InvalidOperationException("Invalid response format from client service.", ex);
        }
    }

}