using ERP.StockService.Application.DTOs;
using ERP.StockService.Domain;
using ERP.StockService.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;

namespace ERP.StockService.Infrastructure.Persistence.Seeders;

/// <summary>
/// Seeds the stock database with fournisseurs, bon entrées, bon sorties, and bon retours.
/// Uses HTTP clients to fetch client and article data from external services.
/// </summary>
public class StockDbSeeder
{
    private readonly StockDbContext _dbContext;
    private readonly IArticleServiceHttpClient _articleServiceHttpClient;
    private readonly IClientServiceHttpClient _clientServiceHttpClient;
    private readonly ILogger<StockDbSeeder>? _logger;

    private List<ArticleResponseDto> _articles = new();
    private List<ClientResponseDto> _clients = new();

    public StockDbSeeder(
        StockDbContext dbContext,
        IArticleServiceHttpClient articleServiceHttpClient,
        IClientServiceHttpClient clientServiceHttpClient,
        ILogger<StockDbSeeder>? logger = null)
    {
        _dbContext = dbContext;
        _articleServiceHttpClient = articleServiceHttpClient;
        _clientServiceHttpClient = clientServiceHttpClient;
        _logger = logger;
    }

    // ════════════════════════════════════════════════════════════════════════
    // ENTRY POINT
    // ════════════════════════════════════════════════════════════════════════

    public async Task SeedAsync()
    {
        _logger?.LogInformation("Starting stock database seeding...");

        // Check if data already exists
        if (await _dbContext.BonEntres.AnyAsync() || await _dbContext.BonSorties.AnyAsync())
        {
            _logger?.LogInformation("Stock data already exists, skipping seed.");
            return;
        }

        // Fetch external data
        await LoadExternalDataAsync();

        // Only proceed if we have data to work with
        if (!_articles.Any() || !_clients.Any())
        {
            _logger?.LogWarning("No articles or clients found. Seeding stock data skipped.");
            return;
        }

        _logger?.LogInformation("Loaded {ArticleCount} articles and {ClientCount} clients", _articles.Count, _clients.Count);

        await SeedBonEntresAsync();
        await SeedBonSortiesAsync();
        await SeedBonRetoursAsync();

        _logger?.LogInformation("Stock database seeding completed successfully.");
    }

    // ════════════════════════════════════════════════════════════════════════
    // LOAD EXTERNAL DATA
    // ════════════════════════════════════════════════════════════════════════

    private async Task LoadExternalDataAsync()
    {
        try
        {
            _logger?.LogInformation("Fetching articles from Article Service...");

            // Get first page of articles
            var articlesPaged = await _articleServiceHttpClient.GetAllPagedAsync(pageNumber: 1, pageSize: 50);
            _articles = articlesPaged.Items?.ToList() ?? new List<ArticleResponseDto>();

            _logger?.LogInformation("Loaded {Count} articles from article service", _articles.Count);

            // Get additional pages if needed
            if (articlesPaged.TotalPages > 1)
            {
                for (int page = 2; page <= Math.Min(articlesPaged.TotalPages, 3); page++)
                {
                    var additionalPage = await _articleServiceHttpClient.GetAllPagedAsync(pageNumber: page, pageSize: 50);
                    _articles.AddRange(additionalPage.Items ?? new List<ArticleResponseDto>());
                }
                _logger?.LogInformation("Total articles loaded: {Count}", _articles.Count);
            }

            _logger?.LogInformation("Fetching clients from Client Service...");

            // Get first page of clients
            var clientsPaged = await _clientServiceHttpClient.GetAllPagedAsync(pageNumber: 1, pageSize: 50);
            _clients = clientsPaged.Items?.ToList() ?? new List<ClientResponseDto>();

            _logger?.LogInformation("Loaded {Count} clients from client service", _clients.Count);

            // Get additional pages if needed
            if (clientsPaged.TotalPages > 1)
            {
                for (int page = 2; page <= Math.Min(clientsPaged.TotalPages, 3); page++)
                {
                    var additionalPage = await _clientServiceHttpClient.GetAllPagedAsync(pageNumber: page, pageSize: 50);
                    _clients.AddRange(additionalPage.Items ?? new List<ClientResponseDto>());
                }
                _logger?.LogInformation("Total clients loaded: {Count}", _clients.Count);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading external data from Article/Client services");
            throw;
        }
    }

    // Helper method to get a specific article by index
    private ArticleResponseDto GetArticleByIndex(int index)
    {
        if (!_articles.Any())
            throw new InvalidOperationException("No articles available for seeding");

        return _articles[index % _articles.Count];
    }

    // Helper method to get a specific client by index
    private ClientResponseDto GetClientByIndex(int index)
    {
        if (!_clients.Any())
            throw new InvalidOperationException("No clients available for seeding");

        return _clients[index % _clients.Count];
    }

    // ════════════════════════════════════════════════════════════════════════
    // FOURNISSEURS (Suppliers - local data, no external dependency)
    // ════════════════════════════════════════════════════════════════════════


    // ════════════════════════════════════════════════════════════════════════
    // BON ENTRES (Goods received from suppliers)
    // ════════════════════════════════════════════════════════════════════════

    private async Task SeedBonEntresAsync()
    {
        if (await _dbContext.BonEntres.AnyAsync())
        {
            _logger?.LogInformation("BonEntres already exist, skipping seed.");
            return;
        }

        _logger?.LogInformation("Seeding bon entrées...");

        var fournisseur1Id= Guid.NewGuid();
        var fournisseur2Id= Guid.NewGuid();


        var article1 = GetArticleByIndex(0);
        var article2 = GetArticleByIndex(1);
        var article3 = GetArticleByIndex(2);

        var be1 = BonEntre.Create("BE-001", fournisseur1Id, "Première livraison Alpha");
        be1.AddLigne(article1.Id, 100, article1.Prix);
        be1.AddLigne(article2.Id, 50, article2.Prix);

        var be2 = BonEntre.Create("BE-002", fournisseur1Id, "Deuxième livraison Alpha");
        be2.AddLigne(article3.Id, 200, article3.Prix);

        var be3 = BonEntre.Create("BE-003", fournisseur2Id, "Livraison Beta");
        be3.AddLigne(article1.Id, 75, article1.Prix);
        be3.AddLigne(article2.Id, 40, article2.Prix);
        be3.AddLigne(article3.Id, 150, article3.Prix);

        // One soft-deleted bon for deleted-list testing
        var be4 = BonEntre.Create("BE-004", fournisseur2Id, "Bon annulé");
        be4.AddLigne(article1.Id, 10, article1.Prix);

        await _dbContext.BonEntres.AddRangeAsync(be1, be2, be3, be4);
        await _dbContext.SaveChangesAsync();

        _logger?.LogInformation("Seeded 4 bon entrées (1 soft-deleted).");
    }

    // ════════════════════════════════════════════════════════════════════════
    // BON SORTIES (Goods shipped to clients)
    // ════════════════════════════════════════════════════════════════════════

    private async Task SeedBonSortiesAsync()
    {
        if (await _dbContext.BonSorties.AnyAsync())
        {
            _logger?.LogInformation("BonSorties already exist, skipping seed.");
            return;
        }

        _logger?.LogInformation("Seeding bon sorties...");

        if (!_clients.Any())
            throw new InvalidOperationException("No clients available for seeding bon sorties.");

        var client1 = GetClientByIndex(0);
        var client2 = GetClientByIndex(1);
        var client3 = GetClientByIndex(2);

        var article1 = GetArticleByIndex(0);
        var article2 = GetArticleByIndex(1);
        var article3 = GetArticleByIndex(2);
        var article4 = GetArticleByIndex(3);

        var bs1 = BonSortie.Create("BS-001", client1.Id, $"Commande {client1.Name}");
        bs1.AddLigne(article1.Id, 10, article1.Prix);
        bs1.AddLigne(article2.Id, 5, article2.Prix);

        var bs2 = BonSortie.Create("BS-002", client1.Id, $"Deuxième commande {client1.Name}");
        bs2.AddLigne(article3.Id, 30, article3.Prix);

        var bs3 = BonSortie.Create("BS-003", client2.Id, $"Commande {client2.Name}");
        bs3.AddLigne(article1.Id, 20, article1.Prix);
        bs3.AddLigne(article2.Id, 8, article2.Prix);

        var bs4 = BonSortie.Create("BS-004", client3.Id, $"Commande {client3.Name}");
        bs4.AddLigne(article4.Id, 15, article4.Prix);

        // One soft-deleted bon for deleted-list testing
        var bs5 = BonSortie.Create("BS-005", client2.Id, "Bon annulé");
        bs5.AddLigne(article1.Id, 5, article1.Prix);

        await _dbContext.BonSorties.AddRangeAsync(bs1, bs2, bs3, bs4, bs5);
        await _dbContext.SaveChangesAsync();

        _logger?.LogInformation("Seeded 5 bon sorties (1 soft-deleted).");
    }

    // ════════════════════════════════════════════════════════════════════════
    // BON RETOURS (Returned goods)
    // ════════════════════════════════════════════════════════════════════════

    private async Task SeedBonRetoursAsync()
    {
        if (await _dbContext.BonRetours.AnyAsync())
        {
            _logger?.LogInformation("BonRetours already exist, skipping seed.");
            return;
        }

        _logger?.LogInformation("Seeding bon retours...");

        var bonSortie = await _dbContext.BonSorties.FirstOrDefaultAsync(b => b.Numero == "BS-001");
        var bonEntre = await _dbContext.BonEntres.FirstOrDefaultAsync(b => b.Numero == "BE-001");

        if (bonSortie == null || bonEntre == null)
            throw new InvalidOperationException("Required BonEntree or BonSortie not found for retour seeding.");

        var article1 = GetArticleByIndex(0);
        var article2 = GetArticleByIndex(1);

        // Retour from a BonSortie (client returns goods)
        var br1 = BonRetour.Create(
            "BR-001",
            bonSortie.Id,
            RetourSourceType.BonSortie,
            "Article défectueux",
            "Retour partiel BS-001");
        br1.AddLigne(article1.Id, 2, article1.Prix);
        br1.AddLigne(article2.Id, 1, article2.Prix);

        // Retour from a BonEntre (returning goods to fournisseur)
        var br2 = BonRetour.Create(
            "BR-002",
            bonEntre.Id,
            RetourSourceType.BonEntre,
            "Marchandise non conforme",
            "Retour partiel BE-001");
        br2.AddLigne(article1.Id, 5, article1.Prix);
        br2.AddLigne(article2.Id, 10, article2.Prix);

        // One soft-deleted bon for deleted-list testing
        var br3 = BonRetour.Create(
            "BR-003",
            bonSortie.Id,
            RetourSourceType.BonSortie,
            "Retour annulé",
            null);
        br3.AddLigne(article1.Id, 1, article1.Prix);


        await _dbContext.BonRetours.AddRangeAsync(br1, br2, br3);
        await _dbContext.SaveChangesAsync();

        _logger?.LogInformation("Seeded 3 bon retours (1 soft-deleted).");
    }
}

// Extension methods for easy registration and seeding
public static class StockDbSeederExtensions
{
    public static IServiceCollection AddStockSeeders(this IServiceCollection services)
    {
        services.AddScoped<StockDbSeeder>();
        return services;
    }

    public static async Task SeedStockDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<StockDbSeeder>();
        await seeder.SeedAsync();
    }
}