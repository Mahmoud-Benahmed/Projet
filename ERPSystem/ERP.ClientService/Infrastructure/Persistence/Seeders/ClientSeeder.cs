using ERP.ClientService.Domain;
using ERP.ClientService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.ClientService.Infrastructure.Persistence.Seeders;

public class ClientSeeder
{
    private readonly ClientDbContext _context;
    private readonly ILogger<ClientSeeder> _logger;

    // System user ID used for AssignedById on category assignments
    private static readonly Guid SystemUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public ClientSeeder(ClientDbContext context, ILogger<ClientSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        if (await _context.Clients.AnyAsync())
        {
            _logger.LogInformation("Clients already seeded — skipping.");
            return;
        }

        // Categories must exist before we can assign them
        var categories = await _context.Categories.ToListAsync();
        if (!categories.Any())
        {
            _logger.LogWarning("No categories found — run CategorySeeder first.");
            return;
        }

        // Build a lookup by code for easy access
        var byCode = categories.ToDictionary(c => c.Code);

        var clients = BuildClients(byCode);

        await _context.Clients.AddRangeAsync(clients);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Seeded {Count} clients.", clients.Count);
    }

    // ── Seed data ─────────────────────────────────────────────────────────────

    private static List<Client> BuildClients(Dictionary<string, Category> byCode)
    {
        var clients = new List<Client>();

        // ── 1. Standard retail client ──────────────────────────────────────────
        var alice = Client.Create(
            name: "Alice Martin",
            email: "alice.martin@example.com",
            address: "12 Rue de la Paix, Tunis 1001",
            phone: "+216 71 000 001",
            taxNumber: null,
            creditLimit: 5_000m,
            DelaiRetour: null);   // falls back to category DelaiRetour

        AssignCategory(alice, byCode, "STD");
        clients.Add(alice);

        // ── 2. VIP client with personal credit override ────────────────────────
        var omar = Client.Create(
            name: "Omar Ben Salah",
            email: "omar.bensalah@acmecorp.tn",
            address: "45 Avenue Habib Bourguiba, Sfax 3000",
            phone: "+216 74 000 002",
            taxNumber: "TN12345678",
            creditLimit: 50_000m,
            DelaiRetour: null);

        AssignCategory(omar, byCode, "VIP");
        clients.Add(omar);

        // ── 3. Wholesale company — bulk pricing, large credit ──────────────────
        var globalTrade = Client.Create(
            name: "Global Trade SARL",
            email: "contact@globaltrade.tn",
            address: "Zone Industrielle, Monastir 5000",
            phone: "+216 73 000 003",
            taxNumber: "TN98765432",
            creditLimit: 200_000m,
            DelaiRetour: null);

        AssignCategory(globalTrade, byCode, "WHL");
        clients.Add(globalTrade);

        // ── 4. Public sector client ────────────────────────────────────────────
        var ministry = Client.Create(
            name: "Ministère de l'Éducation",
            email: "achats@education.gov.tn",
            address: "Boulevard Bab Benat, Tunis 1008",
            phone: "+216 71 000 004",
            taxNumber: "TN00000001",
            creditLimit: 500_000m,
            DelaiRetour: null);

        AssignCategory(ministry, byCode, "PUB");
        clients.Add(ministry);

        // ── 5. Reseller — assigned two categories ──────────────────────────────
        var techResell = Client.Create(
            name: "TechResell Pro",
            email: "info@techresell.tn",
            address: "Centre Urbain Nord, Tunis 1082",
            phone: "+216 71 000 005",
            taxNumber: "TN11223344",
            creditLimit: 80_000m,
            DelaiRetour: null);

        AssignCategory(techResell, byCode, "RSL");
        AssignCategory(techResell, byCode, "WHL");   // also qualifies for wholesale
        clients.Add(techResell);

        // ── 6. New client — minimal profile, short return window ───────────────
        var newbie = Client.Create(
            name: "Yasmine Trabelsi",
            email: "yasmine.trabelsi@gmail.com",
            address: "Cité El Khadra, Tunis 1003",
            phone: null,
            taxNumber: null,
            creditLimit: 1_000m,
            DelaiRetour: null);

        AssignCategory(newbie, byCode, "NEW");
        clients.Add(newbie);

        // ── 7. Client with a personal DelaiRetour override ─────────────────────
        var premiumLoyalty = Client.Create(
            name: "Karim Jebali",
            email: "karim.jebali@premium.tn",
            address: "Les Berges du Lac, Tunis 1053",
            phone: "+216 71 000 007",
            taxNumber: null,
            creditLimit: 30_000m,
            DelaiRetour: 90);   // personal 90-day override — wins over any category

        AssignCategory(premiumLoyalty, byCode, "VIP");
        clients.Add(premiumLoyalty);

        // ── 8. Blocked client ──────────────────────────────────────────────────
        var blocked = Client.Create(
            name: "Riadh Mansouri",
            email: "riadh.mansouri@blocked.tn",
            address: "Bardo, Tunis 2000",
            phone: "+216 71 000 008",
            taxNumber: null,
            creditLimit: 10_000m,
            DelaiRetour: null);

        AssignCategory(blocked, byCode, "STD");
        blocked.Block();   // simulate a client blocked for payment issues
        clients.Add(blocked);

        // ── 9. Soft-deleted client — to test GetPagedDeleted ──────────────────
        var deleted = Client.Create(
            name: "Société Fantôme",
            email: "contact@fantome.tn",
            address: "Adresse inconnue",
            phone: null,
            taxNumber: null,
            creditLimit: null,
            DelaiRetour: null);

        deleted.Delete();
        clients.Add(deleted);

        // ── 10. Client without any category — tests null return window ─────────
        var noCat = Client.Create(
            name: "Slim Bouaziz",
            email: "slim.bouaziz@nocategory.tn",
            address: "Ariana 2080",
            phone: "+216 71 000 010",
            taxNumber: null,
            creditLimit: null,
            DelaiRetour: null);

        clients.Add(noCat);

        return clients;
    }

    // ── Helper — safely assigns a category only if the code exists ────────────
    private static void AssignCategory(
        Client client,
        Dictionary<string, Category> byCode,
        string code)
    {
        if (!byCode.TryGetValue(code, out var category))
            return;  // category not found in DB — skip silently

        if (!category.IsActive)
            return;  // inactive categories cannot be assigned (domain guard)

        client.AddCategory(category, SystemUserId);
    }
}