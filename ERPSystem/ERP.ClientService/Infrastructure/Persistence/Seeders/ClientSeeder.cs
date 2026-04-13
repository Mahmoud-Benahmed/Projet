using ERP.ClientService.Application.DTOs;
using ERP.ClientService.Application.Interfaces;

namespace ERP.ClientService.Infrastructure.Persistence.Seeders;

public class ClientSeeder
{
    private readonly IClientService _clientService;
    private readonly ICategoryService _categoryService;
    private readonly ILogger<ClientSeeder> _logger;

    private static readonly Guid SystemUserId = Guid.NewGuid();

    public ClientSeeder(
        IClientService clientService,
        ICategoryService categoryService,
        ILogger<ClientSeeder> logger)
    {
        _clientService = clientService;
        _categoryService = categoryService;
        _logger = logger;
    }

    public async Task SeedAsync(List<CategoryResponseDto> categories)
    {
        // Check if clients already exist
        var existingClients = await _clientService.GetAllAsync(1, 10);
        if (existingClients.Items.Any())
        {
            _logger.LogInformation("Clients already seeded — skipping.");
            return;
        }

        if (categories == null || !categories.Any())
        {
            _logger.LogError("No categories provided. Cannot seed clients without categories.");
            return;
        }

        // Build lookup by code for easy access
        var byCode = categories.ToDictionary(c => c.Code);

        // Get count of the dictionary (number of key-value pairs)
        int dictCount = byCode.Count;

        var clientRequests = BuildClientRequests(byCode);
        Random random = new Random();

        if (dictCount > 0)
        {
            foreach (var request in clientRequests)
            {
                try
                {
                    int randomIndex = random.Next(dictCount);// random amount of categories to assign (0 to dictCount-1)
                    var indexList= new List<int>();

                    for(int i=0; i<randomIndex; i++)
                    {
                        indexList.Add(random.Next(dictCount));// random index to select a category from the dictionary
                    }

                    var distinctIndexes = indexList.Distinct().ToList(); // Ensure unique category assignments

                    var CategoryIdsToAssign = new List<Guid>();

                    foreach (var index in distinctIndexes)
                    {
                        var categoryId = byCode.Values.ElementAt(index).Id;
                        if(!CategoryIdsToAssign.Contains(categoryId))
                            CategoryIdsToAssign.Add(categoryId);
                    }

                    // Create client via service (publishes event)
                    var client = await _clientService.CreateAsync(request);
                    _logger.LogInformation("Seeded client: {Name} (Email: {Email}, Id: {Id})",
                                                             client.Name, client.Email, client.Id);

                    foreach (var categoryId in CategoryIdsToAssign)
                    {
                        try
                        {
                            await _clientService.AddCategoryAsync(client.Id, categoryId, SystemUserId);
                            var category = byCode.Values.FirstOrDefault(c => c.Id == categoryId);
                            _logger.LogInformation("  Assigned category '{Category}' to client {Client}",
                                category?.Name ?? categoryId.ToString(), client.Name);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "  Failed to assign category {CategoryId} to client {Client}",
                                categoryId, client.Name);
                        }
                    }

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to seed client: {Name}", request.Name);
                }
            }
        }

        // Handle blocked client separately (after creation)
        await CreateBlockedClient(byCode);

        // Handle soft-deleted client separately
        await CreateDeletedClient(byCode);

        _logger.LogInformation("Client seeding completed.");
    }

    private async Task CreateBlockedClient(Dictionary<string, CategoryResponseDto> byCode)
    {
        try
        {
            var blockedRequest = new CreateClientRequestDto(
                Name: "Riadh Mansouri",
                Email: "riadh.mansouri@blocked.tn",
                Address: "Bardo, Tunis 2000",
                Phone: "+216 71 000 008",
                TaxNumber: null,
                CreditLimit: 10000m,
                DelaiRetour: null,
                DuePaymentPeriod: 30);

            var blocked = await _clientService.CreateAsync(blockedRequest);
            await _clientService.BlockAsync(blocked.Id);
            _logger.LogInformation("Seeded and blocked client: {Name}", blocked.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed blocked client");
        }
    }

    private async Task CreateDeletedClient(Dictionary<string, CategoryResponseDto> byCode)
    {
        try
        {
            var deletedRequest = new CreateClientRequestDto(
                Name: "Société Fantôme",
                Email: "contact@fantome.tn",
                Address: "Adresse inconnue",
                Phone: null,
                TaxNumber: null,
                CreditLimit: null,
                DelaiRetour: null,
                DuePaymentPeriod: 30);

            var deleted = await _clientService.CreateAsync(deletedRequest);
            await _clientService.DeleteAsync(deleted.Id);
            _logger.LogInformation("Seeded and soft-deleted client: {Name}", deleted.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed deleted client");
        }
    }

    private List<CreateClientRequestDto> BuildClientRequests(Dictionary<string, CategoryResponseDto> byCode)
    {
        var clients = new List<CreateClientRequestDto>();

        // Helper to get category ID by code
        Guid? GetCategoryId(string code) => byCode.TryGetValue(code, out var cat) ? cat.Id : (Guid?)null;

        // 1. Standard retail client
        clients.Add(new CreateClientRequestDto(
            Name: "Alice Martin",
            Email: "alice.martin@example.com",
            Address: "12 Rue de la Paix, Tunis 1001",
            Phone: "+216 71 000 001",
            TaxNumber: null,
            CreditLimit: 5000m,
            DelaiRetour: null,
            DuePaymentPeriod: 30));

        // 2. VIP client
        clients.Add(new CreateClientRequestDto(
            Name: "Omar Ben Salah",
            Email: "omar.bensalah@acmecorp.tn",
            Address: "45 Avenue Habib Bourguiba, Sfax 3000",
            Phone: "+216 74 000 002",
            TaxNumber: "TN12345678",
            CreditLimit: 50000m,
            DelaiRetour: null,
            DuePaymentPeriod: 45));

        // 3. Wholesale company
        clients.Add(new CreateClientRequestDto(
            Name: "Global Trade SARL",
            Email: "contact@globaltrade.tn",
            Address: "Zone Industrielle, Monastir 5000",
            Phone: "+216 73 000 003",
            TaxNumber: "TN98765432",
            CreditLimit: 200000m,
            DelaiRetour: null,
            DuePaymentPeriod: 60));

        // 4. Public sector client
        clients.Add(new CreateClientRequestDto(
            Name: "Ministère de l'Éducation",
            Email: "achats@education.gov.tn",
            Address: "Boulevard Bab Benat, Tunis 1008",
            Phone: "+216 71 000 004",
            TaxNumber: "TN00000001",
            CreditLimit: 500000m,
            DelaiRetour: null,
            DuePaymentPeriod: 90));

        // 5. Reseller with two categories
        var resellerCategories = new List<Guid>();
        if (GetCategoryId("RSL").HasValue) resellerCategories.Add(GetCategoryId("RSL").Value);
        if (GetCategoryId("WHL").HasValue) resellerCategories.Add(GetCategoryId("WHL").Value);

        clients.Add(new CreateClientRequestDto(
            Name: "TechResell Pro",
            Email: "info@techresell.tn",
            Address: "Centre Urbain Nord, Tunis 1082",
            Phone: "+216 71 000 005",
            TaxNumber: "TN11223344",
            CreditLimit: 80000m,
            DelaiRetour: null,
            DuePaymentPeriod: 45
        ));

        // 6. New client
        clients.Add(new CreateClientRequestDto(
            Name: "Yasmine Trabelsi",
            Email: "yasmine.trabelsi@gmail.com",
            Address: "Cité El Khadra, Tunis 1003",
            Phone: null,
            TaxNumber: null,
            CreditLimit: 1000m,
            DelaiRetour: null,
            DuePaymentPeriod: 15));

        // 7. Client with personal DelaiRetour override
        clients.Add(new CreateClientRequestDto(
            Name: "Karim Jebali",
            Email: "karim.jebali@premium.tn",
            Address: "Les Berges du Lac, Tunis 1053",
            Phone: "+216 71 000 007",
            TaxNumber: null,
            CreditLimit: 30000m,
            DelaiRetour: 90,
            DuePaymentPeriod: 60));

        // 8. Client without any category
        clients.Add(new CreateClientRequestDto(
            Name: "Slim Bouaziz",
            Email: "slim.bouaziz@nocategory.tn",
            Address: "Ariana 2080",
            Phone: "+216 71 000 010",
            TaxNumber: null,
            CreditLimit: null,
            DelaiRetour: null,
            DuePaymentPeriod: 30));

        return clients;
    }
}