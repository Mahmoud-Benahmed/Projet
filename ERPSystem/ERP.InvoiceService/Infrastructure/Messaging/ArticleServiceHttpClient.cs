using ERP.InvoiceService.Application.DTOs;
using System.Net;
using System.Net.Http.Json;

namespace ERP.InvoiceService.Infrastructure.Messaging;

public class ArticleServiceHttpClient : IArticleServiceHttpClient
{
    private readonly HttpClient _httpClient;

    public ArticleServiceHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ArticleResponseDto> GetByIdAsync(Guid articleId)
    {
        var response = await _httpClient.GetAsync($"/articles/{articleId}");

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new KeyNotFoundException($"Article with ID {articleId} not found.");
        }

        response.EnsureSuccessStatusCode();

        // Deserialize JSON to DTO
        var article = await response.Content.ReadFromJsonAsync<ArticleResponseDto>();
        if (article == null)
            throw new InvalidOperationException("Failed to deserialize article response.");

        return article;
    }
}