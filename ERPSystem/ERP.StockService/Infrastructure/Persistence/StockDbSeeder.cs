using ERP.StockService.Application.DTOs;
using ERP.StockService.Application.Interfaces;
using ERP.StockService.Domain.LocalCache.Article;
using ERP.StockService.Domain.LocalCache.Client;
using ERP.StockService.Domain.LocalCache.Fournisseur;
using Microsoft.EntityFrameworkCore;

namespace ERP.StockService.Infrastructure.Persistence.Seeders;

public class StockDbSeeder
{
    private readonly StockDbContext _dbContext;

    private readonly IArticleCacheRepository _articleCacheRepository;
    private readonly IClientCacheRepository _clientCacheRepository;
    private readonly IFournisseurCacheRepository _fournisseurCacheRepository;
    private readonly IBonEntreService _bonEntreService;
    private readonly IBonSortieService _bonSortieService;
    private readonly IBonRetourService _bonRetourService;
    private readonly ILogger<StockDbSeeder>? _logger;

    private List<ArticleCache> _articles = new();
    private List<ClientCache> _clients = new();
    private List<FournisseurCache> _fournisseurs = new();

    public StockDbSeeder(
        StockDbContext dbContext,
        IArticleCacheRepository articleCacheRepository,
        IClientCacheRepository clientCacheRepository,
        IFournisseurCacheRepository fournisseurCacheRepository,
        IBonEntreService bonEntreService,
        IBonSortieService bonSortieService,
        IBonRetourService bonRetourService,
        ILogger<StockDbSeeder>? logger = null)
    {
        _dbContext = dbContext;
        _articleCacheRepository = articleCacheRepository;
        _clientCacheRepository = clientCacheRepository;
        _bonEntreService = bonEntreService;
        _bonSortieService = bonSortieService;
        _bonRetourService = bonRetourService;
        _fournisseurCacheRepository = fournisseurCacheRepository;
        _logger = logger;
    }

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

        _articles= (await _articleCacheRepository.GetAllAsync()).ToList();

        if (!_articles.Any() || !_clients.Any())
        {
            _logger?.LogWarning("No articles or clients found. Seeding stock data skipped.");
            return;
        }

        _logger?.LogInformation("Loaded {ArticleCount} articles and {ClientCount} clients", _articles.Count, _clients.Count);


        // Seed Bon Entrees using service
        await SeedBonEntresUsingServiceAsync();

        // Seed Bon Sorties using service
        await SeedBonSortiesUsingServiceAsync();

        // Seed Bon Retours using service
        await SeedBonRetoursUsingServiceAsync();

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
            _articles = await _articleCacheRepository.GetAllAsync();
            _logger?.LogInformation("Loaded {Count} articles", _articles.Count);

            _logger?.LogInformation("Fetching clients from Client Service...");
            _clients = await _clientCacheRepository.GetAllAsync();
            _logger?.LogInformation("Loaded {Count} clients", _clients.Count);

            _logger?.LogInformation("Fetching fournisseurs from Fournisseurs Service...");
            _fournisseurs = await _fournisseurCacheRepository.GetAllAsync();
            _logger?.LogInformation("Loaded {Count} Fournisseurs", _fournisseurs.Count);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading external data");
            throw;
        }
    }


    // ════════════════════════════════════════════════════════════════════════
    // BON ENTRES using Service (one per article with multiple quantities)
    // ════════════════════════════════════════════════════════════════════════

    private async Task SeedBonEntresUsingServiceAsync()
    {
        _logger?.LogInformation("Seeding bon entrées using service...");

        var random = new Random();

        var bonEntreeGroups = new[]
        {
        new { Fournisseur = _fournisseurs[0], Prefix = "Alpha", Quantities = new[] { 100, 50, 75, 30 } },
        new { Fournisseur = _fournisseurs[1], Prefix = "Beta", Quantities = new[] { 200, 80, 120, 45 } },
        new { Fournisseur = _fournisseurs[2], Prefix = "Gamma", Quantities = new[] { 150, 60, 90, 25 } },
        new { Fournisseur = _fournisseurs[3], Prefix = "Delta", Quantities = new[] { 300, 100, 200, 60 } }
    };

        int articleIndex = 0;

        foreach (var group in bonEntreeGroups)
        {
            for (int beIndex = 1; beIndex <= 2; beIndex++)
            {
                var lignes = new List<LigneRequestDto>();

                for (int i = 0; i < Math.Min(4, _articles.Count - articleIndex); i++)
                {
                    var article = _articles[articleIndex % _articles.Count];
                    var quantity = group.Quantities[i % group.Quantities.Length];
                    var price = article.Prix * (decimal)(0.8 + random.NextDouble() * 0.4);

                    // ✅ CORRECT: Positional constructor
                    lignes.Add(new LigneRequestDto(
                        ArticleId: article.Id,
                        Quantity: quantity,
                        Price: Math.Round(price, 3),
                        Remarque: null
                    ));

                    articleIndex++;
                }

                if (!lignes.Any()) continue;

                // ✅ CORRECT: Positional constructor
                var createDto = new CreateBonEntreRequestDto(
                    FournisseurId: group.Fournisseur.Id,
                    Observation: $"Livraison {group.Prefix} - Batch {beIndex} - {DateTime.UtcNow:yyyy-MM-dd}",
                    Lignes: lignes
                );

                try
                {
                    var result = await _bonEntreService.CreateAsync(createDto);
                    _logger?.LogInformation("Created BonEntre: {Numero} with {LigneCount} lines",
                        result.numero, result.Lignes?.Count ?? 0);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to create BonEntre for fournisseur {Fournisseur}", group.Fournisseur.Name);
                }
            }
        }

        _logger?.LogInformation("Completed seeding bon entrées using service");
    }

    // ════════════════════════════════════════════════════════════════════════
    // BON SORTIES using Service
    // ════════════════════════════════════════════════════════════════════════

    private async Task SeedBonSortiesUsingServiceAsync()
    {
        _logger?.LogInformation("Seeding bon sorties using service...");

        var userId = Guid.NewGuid();
        var random = new Random();

        var existingBonEntrees = await _dbContext.BonEntres
            .Include(b => b.Lignes)
            .Take(3)
            .ToListAsync();

        if (!existingBonEntrees.Any())
        {
            _logger?.LogWarning("No Bon Entrees found to create Bon Sorties from");
            return;
        }

        for (int i = 0; i < _clients.Count && i < 5; i++)
        {
            var client = _clients[i];
            var lignes = new List<LigneRequestDto>();

            var sourceBon = existingBonEntrees[i % existingBonEntrees.Count];
            foreach (var ligne in sourceBon.Lignes.Take(3))
            {
                var article = _articles.FirstOrDefault(a => a.Id == ligne.ArticleId);
                if (article != null)
                {
                    var quantity = Math.Min(ligne.Quantity / 10, 50);
                    if (quantity > 0)
                    {
                        // ✅ CORRECT: Positional constructor
                        lignes.Add(new LigneRequestDto(
                            ArticleId: ligne.ArticleId,
                            Quantity: quantity,
                            Price: article.Prix,
                            Remarque: null
                        ));
                    }
                }
            }

            if (!lignes.Any()) continue;

            // ✅ CORRECT: Positional constructor
            var createDto = new CreateBonSortieRequestDto(
                ClientId: client.Id,
                Observation: $"Commande {client.Name} - {DateTime.UtcNow:yyyy-MM-dd}",
                Lignes: lignes
            );

            try
            {
                var result = await _bonSortieService.CreateAsync(createDto);
                _logger?.LogInformation("Created BonSortie: {Numero} for client {Client}",
                    result.numero, client.Name);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to create BonSortie for client {Client}", client.Name);
            }
        }

        _logger?.LogInformation("Completed seeding bon sorties using service");
    }

    // ════════════════════════════════════════════════════════════════════════
    // BON RETOURS using Service
    // ════════════════════════════════════════════════════════════════════════

    private async Task SeedBonRetoursUsingServiceAsync()
    {
        _logger?.LogInformation("Seeding bon retours using service...");

        var userId = Guid.NewGuid();

        // Get existing Bon Sorties to return from
        var existingBonSorties = await _dbContext.BonSorties
            .Include(b => b.Lignes)
            .Take(2)
            .ToListAsync();

        foreach (var bonSortie in existingBonSorties)
        {
            var lignes = new List<LigneRequestDto>();

            foreach (var ligne in bonSortie.Lignes.Take(2))
            {
                var returnQuantity = ligne.Quantity / 5; // Return 20%
                if (returnQuantity > 0)
                {
                    // ✅ CORRECT: Use positional constructor for record
                    lignes.Add(new LigneRequestDto(
                        ArticleId: ligne.ArticleId,
                        Quantity: returnQuantity,
                        Price: ligne.Price,
                        Remarque: "Retour client"
                    ));
                }
            }

            if (!lignes.Any()) continue;

            // ✅ CORRECT: Use positional constructor
            var createDto = new CreateBonRetourRequestDto(
                SourceId: bonSortie.Id,
                SourceType: RetourSourceType.BonSortie,
                Motif: "Retour client - Article défectueux",
                Observation: $"Retour partiel pour {bonSortie.Numero}",
                Lignes: lignes
            );

            try
            {
                var result = await _bonRetourService.CreateAsync(createDto);
                _logger?.LogInformation("Created BonRetour: {Numero} from {SourceType}",
                    result.Numero, result.SourceType);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to create BonRetour from {Numero}", bonSortie.Numero);
            }
        }

        _logger?.LogInformation("Completed seeding bon retours using service");
    }
}

// Extension methods
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