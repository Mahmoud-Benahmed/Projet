using ERP.InvoiceService.Application.DTOs;
using ERP.InvoiceService.Infrastructure.Messaging;
using InvoiceService.Application.Interfaces;
using InvoiceService.Domain;

namespace InvoiceService.Infrastructure.Seeders
{
    /// <summary>
    /// Seeds the database with realistic invoice data covering all statuses
    /// (DRAFT, UNPAID, PAID, CANCELLED) and soft-delete scenarios.
    /// </summary>
    public class DatabaseSeeder
    {
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IArticleServiceHttpClient _articleServiceHttpClient;
        private readonly IClientServiceHttpClient _clientServiceHttpClient;

        private List<ArticleResponseDto> _articles = new();
        private List<ClientResponseDto> _clients = new();

        public DatabaseSeeder(
            IInvoiceRepository invoiceRepository,
            IArticleServiceHttpClient articleServiceHttpClient,
            IClientServiceHttpClient clientServiceHttpClient)
        {
            _invoiceRepository = invoiceRepository;
            _articleServiceHttpClient = articleServiceHttpClient;
            _clientServiceHttpClient = clientServiceHttpClient;
        }

        // ════════════════════════════════════════════════════════════════════════
        // ENTRY POINT
        // ════════════════════════════════════════════════════════════════════════

        public async Task SeedAsync()
        {
            // Guard: skip if data already exists
            var existing = await _invoiceRepository.GetAllAsync(includeDeleted: true);
            if (existing.Any())
                return;

            // Fetch data from external services
            await LoadExternalDataAsync();

            // Only proceed if we have data to work with
            if (!_articles.Any() || !_clients.Any())
            {
                Console.WriteLine("Warning: No articles or clients found. Seeding skipped.");
                return;
            }

            var invoices = new List<Invoice>();

            invoices.AddRange(BuildDraftInvoices());
            invoices.AddRange(BuildUnpaidInvoices());
            invoices.AddRange(BuildPaidInvoices());
            invoices.AddRange(BuildCancelledInvoices());
            invoices.AddRange(BuildDeletedInvoices());

            foreach (var invoice in invoices)
                await _invoiceRepository.AddAsync(invoice);
        }

        // ════════════════════════════════════════════════════════════════════════
        // LOAD EXTERNAL DATA
        // ════════════════════════════════════════════════════════════════════════

        private async Task LoadExternalDataAsync()
        {
            try
            {
                // Get first page of articles (page 1, page size 20)
                var articlesPaged = await _articleServiceHttpClient.GetAllPagedAsync(pageNumber: 1, pageSize: 20);
                _articles = articlesPaged.Items?.ToList() ?? new List<ArticleResponseDto>();

                Console.WriteLine($"Loaded {_articles.Count} articles from article service");

                // Get first page of clients (page 1, page size 20)
                var clientsPaged = await _clientServiceHttpClient.GetAllPagedAsync(pageNumber: 1, pageSize: 20);
                _clients = clientsPaged.Items?.ToList() ?? new List<ClientResponseDto>();

                Console.WriteLine($"Loaded {_clients.Count} clients from client service");

                // If we need more data, fetch additional pages
                if (articlesPaged.TotalPages > 1)
                {
                    for (int page = 2; page <= Math.Min(articlesPaged.TotalPages, 3); page++) // Max 3 pages
                    {
                        var additionalPage = await _articleServiceHttpClient.GetAllPagedAsync(pageNumber: page, pageSize: 20);
                        _articles.AddRange(additionalPage.Items ?? new List<ArticleResponseDto>());
                    }
                    Console.WriteLine($"Total articles loaded: {_articles.Count}");
                }

                if (clientsPaged.TotalPages > 1)
                {
                    for (int page = 2; page <= Math.Min(clientsPaged.TotalPages, 3); page++) // Max 3 pages
                    {
                        var additionalPage = await _clientServiceHttpClient.GetAllPagedAsync(pageNumber: page, pageSize: 20);
                        _clients.AddRange(additionalPage.Items ?? new List<ClientResponseDto>());
                    }
                    Console.WriteLine($"Total clients loaded: {_clients.Count}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading external data: {ex.Message}");
                throw;
            }
        }

        // Helper method to get a random client
        private ClientResponseDto GetRandomClient()
        {
            if (!_clients.Any())
                throw new InvalidOperationException("No clients available for seeding");

            var random = new Random();
            return _clients[random.Next(_clients.Count)];
        }

        // Helper method to get a random article
        private ArticleResponseDto GetRandomArticle()
        {
            if (!_articles.Any())
                throw new InvalidOperationException("No articles available for seeding");

            var random = new Random();
            return _articles[random.Next(_articles.Count)];
        }

        // Helper method to get a specific article by index (for consistent seeding)
        private ArticleResponseDto GetArticleByIndex(int index)
        {
            if (!_articles.Any())
                throw new InvalidOperationException("No articles available for seeding");

            return _articles[index % _articles.Count];
        }

        // Helper method to get a specific client by index (for consistent seeding)
        private ClientResponseDto GetClientByIndex(int index)
        {
            if (!_clients.Any())
                throw new InvalidOperationException("No clients available for seeding");

            return _clients[index % _clients.Count];
        }

        // ════════════════════════════════════════════════════════════════════════
        // DRAFT INVOICES  (Status = DRAFT, not yet finalized)
        // ════════════════════════════════════════════════════════════════════════

        private IEnumerable<Invoice> BuildDraftInvoices()
        {
            var invoices = new List<Invoice>();
            var client1 = GetClientByIndex(0);
            var client2 = GetClientByIndex(1);
            var article1 = GetArticleByIndex(0);
            var article2 = GetArticleByIndex(1);

            // Draft #1 – single item, minimal
            var draft1 = new Invoice(
                invoiceNumber: "INV-2025-DRAFT-001",
                invoiceDate: new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                dueDate: new DateTime(2025, 7, 1, 0, 0, 0, DateTimeKind.Utc),
                clientId: client1.Id,
                clientFullName: client1.Name,
                clientAddress: client1.Address,
                additionalNotes: "Work in progress – awaiting article confirmation.");

            draft1.AddItem(new InvoiceItem(
                draft1.Id,
                article1.Id,
                article1.Libelle,
                article1.BarCode,
                quantity: 2,
                uniPriceHT: article1.Prix,
                taxRate: article1.TVA));

            invoices.Add(draft1);

            // Draft #2 – multiple items
            if (_articles.Count >= 2)
            {
                var draft2 = new Invoice(
                    invoiceNumber: "INV-2025-DRAFT-002",
                    invoiceDate: new DateTime(2025, 6, 10, 0, 0, 0, DateTimeKind.Utc),
                    dueDate: new DateTime(2025, 7, 10, 0, 0, 0, DateTimeKind.Utc),
                    clientId: client2.Id,
                    clientFullName: client2.Name,
                    clientAddress: client2.Address,
                    additionalNotes: null);

                draft2.AddItem(new InvoiceItem(
                    draft2.Id,
                    article1.Id,
                    article1.Libelle,
                    article1.BarCode,
                    quantity: 4,
                    uniPriceHT: article1.Prix,
                    taxRate: article1.TVA));

                draft2.AddItem(new InvoiceItem(
                    draft2.Id,
                    article2.Id,
                    article2.Libelle,
                    article2.BarCode,
                    quantity: 4,
                    uniPriceHT: article2.Prix,
                    taxRate: article2.TVA));

                invoices.Add(draft2);
            }

            return invoices;
        }

        // ════════════════════════════════════════════════════════════════════════
        // UNPAID INVOICES  (finalized, awaiting payment)
        // ════════════════════════════════════════════════════════════════════════

        private IEnumerable<Invoice> BuildUnpaidInvoices()
        {
            var invoices = new List<Invoice>();

            if (!_clients.Any() || !_articles.Any())
                return invoices;

            var client1 = GetClientByIndex(0);
            var client2 = GetClientByIndex(1);
            var client3 = GetClientByIndex(2);
            var article1 = GetArticleByIndex(0);
            var article2 = GetArticleByIndex(1);
            var article3 = GetArticleByIndex(2);
            var article4 = GetArticleByIndex(3);

            // Unpaid #1 – due soon
            var unpaid1 = new Invoice(
                invoiceNumber: "INV-2025-0047",
                invoiceDate: new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc),
                dueDate: new DateTime(2025, 5, 31, 0, 0, 0, DateTimeKind.Utc),
                clientId: client1.Id,
                clientFullName: client1.Name,
                clientAddress: client1.Address,
                additionalNotes: "Net 30 payment terms apply.");

            unpaid1.AddItem(new InvoiceItem(
                unpaid1.Id,
                article1.Id,
                article1.Libelle,
                article1.BarCode,
                quantity: 10,
                uniPriceHT: article1.Prix,
                taxRate: article1.TVA));

            unpaid1.AddItem(new InvoiceItem(
                unpaid1.Id,
                article2.Id,
                article2.Libelle,
                article2.BarCode,
                quantity: 10,
                uniPriceHT: article2.Prix,
                taxRate: article2.TVA));

            unpaid1.FinalizeInvoice();
            invoices.Add(unpaid1);

            // Unpaid #2 – overdue
            if (_clients.Count >= 3)
            {
                var unpaid2 = new Invoice(
                    invoiceNumber: "INV-2025-0031",
                    invoiceDate: new DateTime(2025, 3, 15, 0, 0, 0, DateTimeKind.Utc),
                    dueDate: new DateTime(2025, 4, 15, 0, 0, 0, DateTimeKind.Utc),
                    clientId: client3.Id,
                    clientFullName: client3.Name,
                    clientAddress: client3.Address,
                    additionalNotes: "OVERDUE – second payment reminder sent.");

                unpaid2.AddItem(new InvoiceItem(
                    unpaid2.Id,
                    article3.Id,
                    article3.Libelle,
                    article3.BarCode,
                    quantity: 5,
                    uniPriceHT: article3.Prix,
                    taxRate: article3.TVA));

                unpaid2.FinalizeInvoice();
                invoices.Add(unpaid2);
            }

            // Unpaid #3 – large order, multiple items
            var unpaid3 = new Invoice(
                invoiceNumber: "INV-2025-0052",
                invoiceDate: new DateTime(2025, 5, 20, 0, 0, 0, DateTimeKind.Utc),
                dueDate: new DateTime(2025, 6, 20, 0, 0, 0, DateTimeKind.Utc),
                clientId: client2.Id,
                clientFullName: client2.Name,
                clientAddress: client2.Address,
                additionalNotes: null);

            unpaid3.AddItem(new InvoiceItem(
                unpaid3.Id,
                article1.Id,
                article1.Libelle,
                article1.BarCode,
                quantity: 5,
                uniPriceHT: article1.Prix,
                taxRate: article1.TVA));

            if (_articles.Count >= 4)
            {
                unpaid3.AddItem(new InvoiceItem(
                    unpaid3.Id,
                    article4.Id,
                    article4.Libelle,
                    article4.BarCode,
                    quantity: 5,
                    uniPriceHT: article4.Prix,
                    taxRate: article4.TVA));
            }

            unpaid3.FinalizeInvoice();
            invoices.Add(unpaid3);

            return invoices;
        }

        // ════════════════════════════════════════════════════════════════════════
        // PAID INVOICES
        // ════════════════════════════════════════════════════════════════════════

        private IEnumerable<Invoice> BuildPaidInvoices()
        {
            var invoices = new List<Invoice>();

            if (!_clients.Any() || !_articles.Any())
                return invoices;

            var client1 = GetClientByIndex(0);
            var client2 = GetClientByIndex(1);
            var client3 = GetClientByIndex(2);
            var article1 = GetArticleByIndex(0);
            var article2 = GetArticleByIndex(1);
            var article3 = GetArticleByIndex(2);

            // Paid #1 – standard order
            var paid1 = new Invoice(
                invoiceNumber: "INV-2025-0018",
                invoiceDate: new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                dueDate: new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                clientId: client1.Id,
                clientFullName: client1.Name,
                clientAddress: client1.Address,
                additionalNotes: "Paid via bank transfer on 2025-02-28.");

            paid1.AddItem(new InvoiceItem(
                paid1.Id,
                article2.Id,
                article2.Libelle,
                article2.BarCode,
                quantity: 20,
                uniPriceHT: article2.Prix,
                taxRate: article2.TVA));

            paid1.FinalizeInvoice();
            paid1.MarkAsPaid();
            invoices.Add(paid1);

            // Paid #2 – different client
            if (_clients.Count >= 3)
            {
                var paid2 = new Invoice(
                    invoiceNumber: "INV-2025-0022",
                    invoiceDate: new DateTime(2025, 2, 15, 0, 0, 0, DateTimeKind.Utc),
                    dueDate: new DateTime(2025, 3, 15, 0, 0, 0, DateTimeKind.Utc),
                    clientId: client3.Id,
                    clientFullName: client3.Name,
                    clientAddress: client3.Address,
                    additionalNotes: "Paid on time.");

                paid2.AddItem(new InvoiceItem(
                    paid2.Id,
                    article1.Id,
                    article1.Libelle,
                    article1.BarCode,
                    quantity: 8,
                    uniPriceHT: article1.Prix,
                    taxRate: 0m)); // zero tax rate for testing

                paid2.FinalizeInvoice();
                paid2.MarkAsPaid();
                invoices.Add(paid2);
            }

            // Paid #3 – high-value order
            var paid3 = new Invoice(
                invoiceNumber: "INV-2025-0035",
                invoiceDate: new DateTime(2025, 4, 1, 0, 0, 0, DateTimeKind.Utc),
                dueDate: new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc),
                clientId: client2.Id,
                clientFullName: client2.Name,
                clientAddress: client2.Address,
                additionalNotes: null);

            paid3.AddItem(new InvoiceItem(
                paid3.Id,
                article3.Id,
                article3.Libelle,
                article3.BarCode,
                quantity: 10,
                uniPriceHT: article3.Prix,
                taxRate: article3.TVA));

            paid3.FinalizeInvoice();
            paid3.MarkAsPaid();
            invoices.Add(paid3);

            return invoices;
        }

        // ════════════════════════════════════════════════════════════════════════
        // CANCELLED INVOICES
        // ════════════════════════════════════════════════════════════════════════

        private IEnumerable<Invoice> BuildCancelledInvoices()
        {
            var invoices = new List<Invoice>();

            if (!_clients.Any() || !_articles.Any())
                return invoices;

            var client1 = GetClientByIndex(0);
            var client2 = GetClientByIndex(1);
            var article1 = GetArticleByIndex(0);

            // Cancelled from DRAFT
            var cancelled1 = new Invoice(
                invoiceNumber: "INV-2025-0009",
                invoiceDate: new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc),
                dueDate: new DateTime(2025, 2, 10, 0, 0, 0, DateTimeKind.Utc),
                clientId: client1.Id,
                clientFullName: client1.Name,
                clientAddress: client1.Address,
                additionalNotes: "Cancelled – client changed requirements before finalization.");

            cancelled1.AddItem(new InvoiceItem(
                cancelled1.Id,
                article1.Id,
                article1.Libelle,
                article1.BarCode,
                quantity: 3,
                uniPriceHT: article1.Prix,
                taxRate: article1.TVA));

            cancelled1.CancelInvoice();
            invoices.Add(cancelled1);

            // Cancelled from UNPAID
            if (_clients.Count >= 2)
            {
                var cancelled2 = new Invoice(
                    invoiceNumber: "INV-2025-0014",
                    invoiceDate: new DateTime(2025, 1, 20, 0, 0, 0, DateTimeKind.Utc),
                    dueDate: new DateTime(2025, 2, 20, 0, 0, 0, DateTimeKind.Utc),
                    clientId: client2.Id,
                    clientFullName: client2.Name,
                    clientAddress: client2.Address,
                    additionalNotes: "Cancelled – order dispute with client.");

                cancelled2.AddItem(new InvoiceItem(
                    cancelled2.Id,
                    article1.Id,
                    article1.Libelle,
                    article1.BarCode,
                    quantity: 6,
                    uniPriceHT: article1.Prix,
                    taxRate: article1.TVA));

                cancelled2.FinalizeInvoice();
                cancelled2.CancelInvoice();
                invoices.Add(cancelled2);
            }

            return invoices;
        }

        // ════════════════════════════════════════════════════════════════════════
        // SOFT-DELETED INVOICES  (IsDeleted = true)
        // ════════════════════════════════════════════════════════════════════════

        private IEnumerable<Invoice> BuildDeletedInvoices()
        {
            var invoices = new List<Invoice>();

            if (!_clients.Any() || !_articles.Any())
                return invoices;

            var client1 = GetClientByIndex(0);
            var client2 = GetClientByIndex(1);
            var article1 = GetArticleByIndex(0);
            var article2 = GetArticleByIndex(1);

            // Deleted draft – entered by mistake
            var deleted1 = new Invoice(
                invoiceNumber: "INV-2025-VOID-001",
                invoiceDate: new DateTime(2025, 4, 5, 0, 0, 0, DateTimeKind.Utc),
                dueDate: new DateTime(2025, 5, 5, 0, 0, 0, DateTimeKind.Utc),
                clientId: client1.Id,
                clientFullName: client1.Name,
                clientAddress: client1.Address,
                additionalNotes: "Created by mistake – soft deleted.");

            deleted1.AddItem(new InvoiceItem(
                deleted1.Id,
                article1.Id,
                article1.Libelle,
                article1.BarCode,
                quantity: 1,
                uniPriceHT: article1.Prix,
                taxRate: article1.TVA));

            deleted1.Delete();
            invoices.Add(deleted1);

            // Deleted paid invoice – archived
            var deleted2 = new Invoice(
                invoiceNumber: "INV-2024-0112",
                invoiceDate: new DateTime(2024, 12, 1, 0, 0, 0, DateTimeKind.Utc),
                dueDate: new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                clientId: client2.Id,
                clientFullName: client2.Name,
                clientAddress: client2.Address,
                additionalNotes: "FY-2024 archived invoice.");

            deleted2.AddItem(new InvoiceItem(
                deleted2.Id,
                article2.Id,
                article2.Libelle,
                article2.BarCode,
                quantity: 3,
                uniPriceHT: article2.Prix,
                taxRate: article2.TVA));

            deleted2.FinalizeInvoice();
            deleted2.MarkAsPaid();
            deleted2.Delete();
            invoices.Add(deleted2);

            return invoices;
        }
    }

    public static class DatabaseSeederExtensions
    {
        public static IServiceCollection AddDatabaseSeeders(
            this IServiceCollection services)
        {
            services.AddScoped<DatabaseSeeder>();
            return services;
        }

        public static async Task SeedDatabaseAsync(this IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
            await seeder.SeedAsync();
        }
    }
}