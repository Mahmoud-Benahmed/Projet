using ERP.StockService.Application.DTOs;
using System.Net;
using System.Text.Json;
namespace ERP.StockService.Infrastructure.Messaging;

public class ArticleServiceHttpClient : IArticleServiceHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ArticleServiceHttpClient>? _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ArticleServiceHttpClient(HttpClient httpClient, ILogger<ArticleServiceHttpClient>? logger = null)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ArticleResponseDto> GetByIdAsync(Guid articleId)
    {
        try
        {
            _logger?.LogInformation("Fetching article {ArticleId} from API", articleId);

            var response = await _httpClient.GetAsync($"/articles/{articleId}");

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger?.LogWarning("Article {ArticleId} not found", articleId);
                throw new KeyNotFoundException($"Article with ID {articleId} not found.");
            }

            response.EnsureSuccessStatusCode();

            // Deserialize JSON to DTO
            var article = await response.Content.ReadFromJsonAsync<ArticleResponseDto>(_jsonOptions);

            if (article == null)
            {
                _logger?.LogError("Failed to deserialize article response for ID {ArticleId}", articleId);
                throw new InvalidOperationException($"Failed to deserialize article response for ID {articleId}.");
            }

            _logger?.LogInformation("Successfully fetched article {ArticleId}: {ArticleName}", articleId, article.Libelle);
            return article;
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogError(ex, "Network error while fetching article {ArticleId}", articleId);
            throw new InvalidOperationException($"Network error while fetching article {articleId}. Please check if the article service is available.", ex);
        }
        catch (JsonException ex)
        {
            _logger?.LogError(ex, "JSON deserialization error for article {ArticleId}", articleId);
            throw new InvalidOperationException($"Invalid response format from article service for ID {articleId}.", ex);
        }
    }

    public async Task<PagedResultDto<ArticleResponseDto>> GetAllPagedAsync(int pageNumber = 1, int pageSize = 10)
    {
        try
        {
            // Validate parameters
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100; // Max page size limit

            _logger?.LogInformation("Fetching articles page {PageNumber} with size {PageSize}", pageNumber, pageSize);

            var response = await _httpClient.GetAsync($"/articles?pageNumber={pageNumber}&pageSize={pageSize}");

            if (!response.IsSuccessStatusCode)
            {
                _logger?.LogWarning("Failed to fetch articles. Status code: {StatusCode}", response.StatusCode);

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return new PagedResultDto<ArticleResponseDto>(new List<ArticleResponseDto>(), 0, pageNumber, pageSize);
                }

                response.EnsureSuccessStatusCode();
            }

            var pagedResult = await response.Content.ReadFromJsonAsync<PagedResultDto<ArticleResponseDto>>(_jsonOptions);

            if (pagedResult == null)
            {
                _logger?.LogWarning("Received null response from article service");
                return new PagedResultDto<ArticleResponseDto>(new List<ArticleResponseDto>(), 0, pageNumber, pageSize);
            }

            _logger?.LogInformation("Successfully fetched {Count} articles out of {TotalCount}",
                pagedResult.Items.Count, pagedResult.TotalCount);

            return pagedResult;
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogError(ex, "Network error while fetching paged articles");
            throw new InvalidOperationException("Network error while fetching articles. Please check if the article service is available.", ex);
        }
        catch (JsonException ex)
        {
            _logger?.LogError(ex, "JSON deserialization error while fetching paged articles");
            throw new InvalidOperationException("Invalid response format from article service.", ex);
        }
    }
}