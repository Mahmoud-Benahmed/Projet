using ERP.ClientService.Domain;
using ERP.ClientService.Infrastructure.Persistence;

namespace ERP.ClientService.Infrastructure.Seeders
{
    public static class ClientSeeder
    {
        public static async Task SeedAsync(ClientDbContext context)
        {
            if (context.Clients.Any())
                return;

            var clients = new List<Client>
            {
                // ── Individuals
                new Client(
                    ClientType.Individual,
                    "Adam Benali",
                    "adam.benali@gmail.com",
                    "12 Rue de la Paix, Tunis 1001",
                    "+216 71 234 567",
                    null
                ),
                new Client(
                    ClientType.Individual,
                    "Yasmine Trabelsi",
                    "yasmine.trabelsi@outlook.com",
                    "45 Avenue de Carthage, Sfax 3000",
                    "+216 74 456 789",
                    null
                ),
                new Client(
                    ClientType.Individual,
                    "Mehdi Chaabane",
                    "mehdi.chaabane@gmail.com",
                    "8 Rue Ibn Khaldoun, Sousse 4000",
                    "+216 73 987 654",
                    null
                ),
                new Client(
                    ClientType.Individual,
                    "Sonia Mansouri",
                    "sonia.mansouri@yahoo.fr",
                    "22 Avenue Habib Bourguiba, Bizerte 7000",
                    "+216 72 112 233",
                    null
                ),
                new Client(
                    ClientType.Individual,
                    "Karim Laabidi",
                    "karim.laabidi@hotmail.com",
                    "17 Rue de Marseille, Tunis 1002",
                    "+216 71 445 566",
                    null
                ),

                // ── Companies
                new Client(
                    ClientType.Company,
                    "Alpha Tech SARL",
                    "info@alphatech.tn",
                    "Zone Industrielle, Sousse 4000",
                    "+216 73 112 233",
                    "TN-7654321-B"
                ),
                new Client(
                    ClientType.Company,
                    "Société Générale Tunisie",
                    "contact@sgt.com.tn",
                    "Avenue Habib Bourguiba, Tunis 1000",
                    "+216 71 890 123",
                    "TN-1234567-A"
                ),
                new Client(
                    ClientType.Company,
                    "MedTech Solutions",
                    "contact@medtech.tn",
                    "Rue du Lac Windermere, Les Berges du Lac, Tunis 1053",
                    "+216 71 962 000",
                    "TN-9876543-C"
                ),
                new Client(
                    ClientType.Company,
                    "Tunisie Telecom Entreprises",
                    "entreprises@tt.tn",
                    "Avenue de la Liberté, Tunis 1002",
                    "+216 71 801 000",
                    "TN-1122334-D"
                ),
                new Client(
                    ClientType.Company,
                    "Green Energy SARL",
                    "info@greenenergy.tn",
                    "Zone Industrielle Bir El Bey, Ben Arous 2013",
                    "+216 71 388 100",
                    "TN-5566778-E"
                ),
            };

            await context.Clients.AddRangeAsync(clients);
            await context.SaveChangesAsync();
        }
    }
}