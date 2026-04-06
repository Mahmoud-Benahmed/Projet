using ERP.StockService.Application.Exceptions;
using System.Net;
namespace ERP.StockService.Infrastructure.Messaging;

public class ArticleServiceClient : IArticleService
{
    private readonly HttpClient _httpClient;

    public ArticleServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task ExistsByIdAsync(Guid articleId)
    {
        var response = await _httpClient.GetAsync($"/articles/{articleId}");

        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new ArticleNotFoundException(articleId);

        response.EnsureSuccessStatusCode();
    }
}