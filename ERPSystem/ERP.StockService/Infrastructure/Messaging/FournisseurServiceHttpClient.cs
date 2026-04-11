using ERP.StockService.Application.DTOs;
using System.Net;
using System.Text.Json;

namespace ERP.InvoiceService.Infrastructure.Messaging;

public class FournisseurServiceHttpClient : IFournisseurServiceHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FournisseurServiceHttpClient>? _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private const string BaseUrl = "/fournisseurs";

    public FournisseurServiceHttpClient(HttpClient httpClient, ILogger<FournisseurServiceHttpClient>? logger = null)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<FournisseurResponseDto> GetByIdAsync(Guid fournisseurId)
    {
        try
        {
            _logger?.LogInformation("Fetching fournisseur {FournisseurId} from API", fournisseurId);

            var response = await _httpClient.GetAsync($"{BaseUrl}/{fournisseurId}");

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger?.LogWarning("Fournisseur {FournisseurId} not found", fournisseurId);
                throw new KeyNotFoundException($"Fournisseur with ID {fournisseurId} not found.");
            }

            response.EnsureSuccessStatusCode();

            // Deserialize JSON to DTO
            var fournisseur = await response.Content.ReadFromJsonAsync<FournisseurResponseDto>(_jsonOptions);

            if (fournisseur == null)
            {
                _logger?.LogError("Failed to deserialize response for ID {FournisseurId}", fournisseurId);
                throw new InvalidOperationException($"Failed to deserialize response for ID {fournisseurId}.");
            }

            _logger?.LogInformation("Successfully fetched fournisseur {FournisseurId}: {FournisseurName}",
                fournisseurId, fournisseur.Name);
            return fournisseur;
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogError(ex, "Network error while fetching fournisseur {FournisseurId}", fournisseurId);
            throw new InvalidOperationException($"Network error while fetching fournisseur {fournisseurId}. Please check if the fournisseur service is available.", ex);
        }
        catch (JsonException ex)
        {
            _logger?.LogError(ex, "JSON deserialization error for fournisseur {FournisseurId}", fournisseurId);
            throw new InvalidOperationException($"Invalid response format from fournisseur service for ID {fournisseurId}.", ex);
        }
    }

    public async Task<PagedResultDto<FournisseurResponseDto>> GetAllPagedAsync(int pageNumber = 1, int pageSize = 10)
    {
        try
        {
            // Validate parameters
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100; // Max page size limit

            _logger?.LogInformation("Fetching fournisseurs page {PageNumber} with size {PageSize}", pageNumber, pageSize);

            var response = await _httpClient.GetAsync($"{BaseUrl}?pageNumber={pageNumber}&pageSize={pageSize}");

            if (!response.IsSuccessStatusCode)
            {
                _logger?.LogWarning("Failed to fetch fournisseurs. Status code: {StatusCode}", response.StatusCode);

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return new PagedResultDto<FournisseurResponseDto>(new List<FournisseurResponseDto>(), 0, pageNumber, pageSize);
                }

                response.EnsureSuccessStatusCode();
            }

            var pagedResult = await response.Content.ReadFromJsonAsync<PagedResultDto<FournisseurResponseDto>>(_jsonOptions);

            if (pagedResult == null)
            {
                _logger?.LogWarning("Received null response from fournisseur service");
                return new PagedResultDto<FournisseurResponseDto>(new List<FournisseurResponseDto>(), 0, pageNumber, pageSize);
            }

            _logger?.LogInformation("Successfully fetched {Count} fournisseurs out of {TotalCount}",
                pagedResult.Items.Count, pagedResult.TotalCount);

            return pagedResult;
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogError(ex, "Network error while fetching paged fournisseurs");
            throw new InvalidOperationException("Network error while fetching fournisseurs. Please check if the fournisseur service is available.", ex);
        }
        catch (JsonException ex)
        {
            _logger?.LogError(ex, "JSON deserialization error while fetching paged fournisseurs");
            throw new InvalidOperationException("Invalid response format from fournisseur service.", ex);
        }
    }
}