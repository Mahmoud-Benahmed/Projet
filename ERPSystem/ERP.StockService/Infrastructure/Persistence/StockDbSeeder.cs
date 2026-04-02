using ERP.StockService.Domain;
using ERP.StockService.Infrastructure.Persistence;

namespace ERP.StockService.Infrastructure.Persistence.Seeders;

public static class StockDbSeeder
{
    // =========================
    // FIXED EXTERNAL IDs
    // (Clients & Articles live in other services)
    // =========================
    private static readonly Guid Client1Id = Guid.NewGuid();
    private static readonly Guid Client2Id = Guid.NewGuid();

    private static readonly Guid Article1Id = Guid.NewGuid();
    private static readonly Guid Article2Id = Guid.NewGuid();
    private static readonly Guid Article3Id = Guid.NewGuid();

    public static async Task SeedAsync(StockDbContext db)
    {
        await SeedFournisseursAsync(db);
        await SeedBonEntresAsync(db);
        await SeedBonSortiesAsync(db);
        await SeedBonRetoursAsync(db);
    }

    // =========================
    // FOURNISSEURS
    // =========================
    private static async Task SeedFournisseursAsync(StockDbContext db)
    {
        if (db.Fournisseurs.Any()) return;

        var fournisseurs = new[]
        {
            Fournisseur.Create(
                name:       "Fournisseur Alpha",
                address:    "12 Rue de la Paix, Tunis",
                phone:      "+21620000001",
                taxNumber:  "TAX-ALPHA-001",
                rib:        "RIB-ALPHA-001-0000",
                email:      "alpha@supplier.com"),

            Fournisseur.Create(
                name:       "Fournisseur Beta",
                address:    "45 Avenue Habib Bourguiba, Sfax",
                phone:      "+21620000002",
                taxNumber:  "TAX-BETA-002",
                rib:        "RIB-BETA-002-0000",
                email:      "beta@supplier.com"),

            Fournisseur.Create(
                name:       "Fournisseur Gamma",
                address:    "7 Rue Ibn Khaldoun, Sousse",
                phone:      "+21620000003",
                taxNumber:  "TAX-GAMMA-003",
                rib:        "RIB-GAMMA-003-0000",
                email:      "gamma@supplier.com"),
        };

        // Gamma is blocked for realistic test coverage
        fournisseurs[2].Block();

        await db.Fournisseurs.AddRangeAsync(fournisseurs);
        await db.SaveChangesAsync();
    }

    // =========================
    // BON ENTRES
    // =========================
    private static async Task SeedBonEntresAsync(StockDbContext db)
    {
        if (db.BonEntres.Any()) return;

        var fournisseur1 = db.Fournisseurs.First(f => f.Name == "Fournisseur Alpha");
        var fournisseur2 = db.Fournisseurs.First(f => f.Name == "Fournisseur Beta");

        var be1 = BonEntre.Create("BE-001", fournisseur1, "Première livraison Alpha");
        be1.AddLigne(Article1Id, 100, 15.00m);
        be1.AddLigne(Article2Id, 50, 30.00m);

        var be2 = BonEntre.Create("BE-002", fournisseur1, "Deuxième livraison Alpha");
        be2.AddLigne(Article3Id, 200, 8.50m);

        var be3 = BonEntre.Create("BE-003", fournisseur2, "Livraison Beta");
        be3.AddLigne(Article1Id, 75, 18.00m);
        be3.AddLigne(Article2Id, 40, 32.00m);
        be3.AddLigne(Article3Id, 150, 9.00m);

        // One soft-deleted bon for deleted-list testing
        var be4 = BonEntre.Create("BE-004", fournisseur2, "Bon annulé");
        be4.AddLigne(Article1Id, 10, 15.00m);       be4.Delete();

        await db.BonEntres.AddRangeAsync(be1, be2, be3, be4);
        await db.SaveChangesAsync();
    }

    // =========================
    // BON SORTIES
    // =========================
    private static async Task SeedBonSortiesAsync(StockDbContext db)
    {
        if (db.BonSorties.Any()) return;

        var bs1 = BonSortie.Create("BS-001", Client1Id, "Commande client 1");
        bs1.AddLigne(Article1Id, 10, 20.00m);
        bs1.AddLigne(Article2Id, 5, 45.00m);

        var bs2 = BonSortie.Create("BS-002", Client1Id, "Deuxième commande client 1");
        bs2.AddLigne(Article3Id, 30, 12.00m);

        var bs3 = BonSortie.Create("BS-003", Client2Id, "Commande client 2");
        bs3.AddLigne(Article1Id, 20, 22.00m);
        bs3.AddLigne(Article2Id, 8, 47.00m);

        // One soft-deleted bon for deleted-list testing
        var bs4 = BonSortie.Create("BS-004", Client2Id, "Bon annulé");
        bs4.AddLigne(Article1Id, 5, 20.00m);
        bs4.Delete();

        await db.BonSorties.AddRangeAsync(bs1, bs2, bs3, bs4);
        await db.SaveChangesAsync();
    }

    // =========================
    // BON RETOURS
    // =========================
    private static async Task SeedBonRetoursAsync(StockDbContext db)
    {
        if (db.BonRetours.Any()) return;

        var bonSortie = db.BonSorties.First(b => b.Numero == "BS-001");
        var bonEntre = db.BonEntres.First(b => b.Numero == "BE-001");

        // Retour from a BonSortie (client returns goods)
        var br1 = BonRetour.Create(
            "BR-001",
            bonSortie.Id,
            RetourSourceType.BonSortie,
            "Article défectueux",
            "Retour partiel BS-001");
        br1.AddLigne(Article1Id, 2, 20.00m); // returned 2 out of 10
        br1.AddLigne(Article2Id, 1, 45.00m); // returned 1 out of 5

        // Retour from a BonEntre (returning goods to fournisseur)
        var br2 = BonRetour.Create(
            "BR-002",
            bonEntre.Id,
            RetourSourceType.BonEntre,
            "Marchandise non conforme",
            "Retour partiel BE-001");
        br2.AddLigne(Article1Id, 5, 15.00m); // returned 5 out of 100
        br2.AddLigne(Article2Id, 10, 30.00m); // returned 10 out of 50

        // One soft-deleted bon for deleted-list testing
        var br3 = BonRetour.Create(
            "BR-003",
            bonSortie.Id,
            RetourSourceType.BonSortie,
            "Retour annulé",
            null);
        br3.AddLigne(Article1Id, 1, 20.00m);
        br3.Delete();

        await db.BonRetours.AddRangeAsync(br1, br2, br3);
        await db.SaveChangesAsync();
    }
}