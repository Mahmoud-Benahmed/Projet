using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InvoiceService.Domain;
using InvoiceService.Application.Interfaces;

namespace InvoiceService.Infrastructure.Seeders
{
    /// <summary>
    /// Seeds the database with realistic invoice data covering all statuses
    /// (DRAFT, UNPAID, PAID, CANCELLED) and soft-delete scenarios.
    /// </summary>
    public class InvoiceSeeder
    {
        private readonly IInvoiceRepository _invoiceRepository;

        // ── Well-known client IDs (shared across seeds for consistency) ──────────
        private static readonly Guid ClientAlphaId = new("11111111-1111-1111-1111-111111111111");
        private static readonly Guid ClientBetaId = new("22222222-2222-2222-2222-222222222222");
        private static readonly Guid ClientGammaId = new("33333333-3333-3333-3333-333333333333");

        // ── Well-known article IDs ───────────────────────────────────────────────
        private static readonly Guid ArticleLaptopId = new("aaaa0001-0000-0000-0000-000000000000");
        private static readonly Guid ArticleMouseId = new("aaaa0002-0000-0000-0000-000000000000");
        private static readonly Guid ArticleKeyboardId = new("aaaa0003-0000-0000-0000-000000000000");
        private static readonly Guid ArticleMonitorId = new("aaaa0004-0000-0000-0000-000000000000");
        private static readonly Guid ArticleHeadsetId = new("aaaa0005-0000-0000-0000-000000000000");
        private static readonly Guid ArticleDockId = new("aaaa0006-0000-0000-0000-000000000000");
        private static readonly Guid ArticleWebcamId = new("aaaa0007-0000-0000-0000-000000000000");

        public InvoiceSeeder(IInvoiceRepository invoiceRepository)
        {
            _invoiceRepository = invoiceRepository;
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
        // DRAFT INVOICES  (Status = DRAFT, not yet finalized)
        // ════════════════════════════════════════════════════════════════════════

        private IEnumerable<Invoice> BuildDraftInvoices()
        {
            // Draft #1 – single item, minimal
            var draft1 = new Invoice(
                invoiceNumber: "INV-2025-DRAFT-001",
                invoiceDate: new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                dueDate: new DateTime(2025, 7, 1, 0, 0, 0, DateTimeKind.Utc),
                clientId: ClientAlphaId,
                clientFullName: "Alpha Tech SARL",
                clientAddress: "12 Avenue de la République, Tunis 1001",
                additionalNotes: "Work in progress – awaiting article confirmation.");

            draft1.AddItem(new InvoiceItem(
                draft1.Id, ArticleLaptopId,
                "Laptop Pro 15\" 32GB", "LP15-32GB",
                quantity: 2, uniPriceHT: 1_800m, taxRate: 0.19m));

            // Draft #2 – multiple items, no notes
            var draft2 = new Invoice(
                invoiceNumber: "INV-2025-DRAFT-002",
                invoiceDate: new DateTime(2025, 6, 10, 0, 0, 0, DateTimeKind.Utc),
                dueDate: new DateTime(2025, 7, 10, 0, 0, 0, DateTimeKind.Utc),
                clientId: ClientBetaId,
                clientFullName: "Beta Solutions Ltd.",
                clientAddress: "55 Rue Ibn Khaldoun, Sfax 3000");

            draft2.AddItem(new InvoiceItem(
                draft2.Id, ArticleMonitorId,
                "27\" 4K Monitor", "MON-4K-27",
                quantity: 4, uniPriceHT: 450m, taxRate: 0.19m));

            draft2.AddItem(new InvoiceItem(
                draft2.Id, ArticleDockId,
                "USB-C Docking Station", "DOCK-USBC",
                quantity: 4, uniPriceHT: 120m, taxRate: 0.19m));

            return new[] { draft1, draft2 };
        }

        // ════════════════════════════════════════════════════════════════════════
        // UNPAID INVOICES  (finalized, awaiting payment)
        // ════════════════════════════════════════════════════════════════════════

        private IEnumerable<Invoice> BuildUnpaidInvoices()
        {
            // Unpaid #1 – due soon
            var unpaid1 = new Invoice(
                invoiceNumber: "INV-2025-0047",
                invoiceDate: new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc),
                dueDate: new DateTime(2025, 5, 31, 0, 0, 0, DateTimeKind.Utc),
                clientId: ClientAlphaId,
                clientFullName: "Alpha Tech SARL",
                clientAddress: "12 Avenue de la République, Tunis 1001",
                additionalNotes: "Net 30 payment terms apply.");

            unpaid1.AddItem(new InvoiceItem(
                unpaid1.Id, ArticleKeyboardId,
                "Mechanical Keyboard TKL", "KB-MECH-TKL",
                quantity: 10, uniPriceHT: 85m, taxRate: 0.19m));

            unpaid1.AddItem(new InvoiceItem(
                unpaid1.Id, ArticleMouseId,
                "Wireless Ergonomic Mouse", "MSE-ERG-WL",
                quantity: 10, uniPriceHT: 55m, taxRate: 0.19m));

            unpaid1.FinalizeInvoice();

            // Unpaid #2 – overdue
            var unpaid2 = new Invoice(
                invoiceNumber: "INV-2025-0031",
                invoiceDate: new DateTime(2025, 3, 15, 0, 0, 0, DateTimeKind.Utc),
                dueDate: new DateTime(2025, 4, 15, 0, 0, 0, DateTimeKind.Utc),
                clientId: ClientGammaId,
                clientFullName: "Gamma Consulting Group",
                clientAddress: "8 Rue de Marseille, Sousse 4000",
                additionalNotes: "OVERDUE – second payment reminder sent.");

            unpaid2.AddItem(new InvoiceItem(
                unpaid2.Id, ArticleHeadsetId,
                "Noise-Cancelling Headset BT", "HS-NC-BT",
                quantity: 5, uniPriceHT: 200m, taxRate: 0.19m));

            unpaid2.FinalizeInvoice();

            // Unpaid #3 – large order, multiple items
            var unpaid3 = new Invoice(
                invoiceNumber: "INV-2025-0052",
                invoiceDate: new DateTime(2025, 5, 20, 0, 0, 0, DateTimeKind.Utc),
                dueDate: new DateTime(2025, 6, 20, 0, 0, 0, DateTimeKind.Utc),
                clientId: ClientBetaId,
                clientFullName: "Beta Solutions Ltd.",
                clientAddress: "55 Rue Ibn Khaldoun, Sfax 3000");

            unpaid3.AddItem(new InvoiceItem(
                unpaid3.Id, ArticleLaptopId,
                "Laptop Pro 15\" 32GB", "LP15-32GB",
                quantity: 5, uniPriceHT: 1_800m, taxRate: 0.19m));

            unpaid3.AddItem(new InvoiceItem(
                unpaid3.Id, ArticleMonitorId,
                "27\" 4K Monitor", "MON-4K-27",
                quantity: 5, uniPriceHT: 450m, taxRate: 0.19m));

            unpaid3.AddItem(new InvoiceItem(
                unpaid3.Id, ArticleWebcamId,
                "HD Webcam 1080p", "WC-1080P",
                quantity: 5, uniPriceHT: 75m, taxRate: 0.19m));

            unpaid3.FinalizeInvoice();

            return new[] { unpaid1, unpaid2, unpaid3 };
        }

        // ════════════════════════════════════════════════════════════════════════
        // PAID INVOICES
        // ════════════════════════════════════════════════════════════════════════

        private IEnumerable<Invoice> BuildPaidInvoices()
        {
            // Paid #1 – standard order
            var paid1 = new Invoice(
                invoiceNumber: "INV-2025-0018",
                invoiceDate: new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                dueDate: new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                clientId: ClientAlphaId,
                clientFullName: "Alpha Tech SARL",
                clientAddress: "12 Avenue de la République, Tunis 1001",
                additionalNotes: "Paid via bank transfer on 2025-02-28.");

            paid1.AddItem(new InvoiceItem(
                paid1.Id, ArticleMouseId,
                "Wireless Ergonomic Mouse", "MSE-ERG-WL",
                quantity: 20, uniPriceHT: 55m, taxRate: 0.19m));

            paid1.FinalizeInvoice();
            paid1.MarkAsPaid();

            // Paid #2 – zero-tax items (e.g. exported goods)
            var paid2 = new Invoice(
                invoiceNumber: "INV-2025-0022",
                invoiceDate: new DateTime(2025, 2, 15, 0, 0, 0, DateTimeKind.Utc),
                dueDate: new DateTime(2025, 3, 15, 0, 0, 0, DateTimeKind.Utc),
                clientId: ClientGammaId,
                clientFullName: "Gamma Consulting Group",
                clientAddress: "8 Rue de Marseille, Sousse 4000",
                additionalNotes: "Export invoice – VAT 0%.");

            paid2.AddItem(new InvoiceItem(
                paid2.Id, ArticleDockId,
                "USB-C Docking Station", "DOCK-USBC",
                quantity: 8, uniPriceHT: 120m, taxRate: 0m));  // zero tax

            paid2.FinalizeInvoice();
            paid2.MarkAsPaid();

            // Paid #3 – high-value order
            var paid3 = new Invoice(
                invoiceNumber: "INV-2025-0035",
                invoiceDate: new DateTime(2025, 4, 1, 0, 0, 0, DateTimeKind.Utc),
                dueDate: new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc),
                clientId: ClientBetaId,
                clientFullName: "Beta Solutions Ltd.",
                clientAddress: "55 Rue Ibn Khaldoun, Sfax 3000");

            paid3.AddItem(new InvoiceItem(
                paid3.Id, ArticleLaptopId,
                "Laptop Pro 15\" 32GB", "LP15-32GB",
                quantity: 10, uniPriceHT: 1_800m, taxRate: 0.19m));

            paid3.AddItem(new InvoiceItem(
                paid3.Id, ArticleHeadsetId,
                "Noise-Cancelling Headset BT", "HS-NC-BT",
                quantity: 10, uniPriceHT: 200m, taxRate: 0.19m));

            paid3.FinalizeInvoice();
            paid3.MarkAsPaid();

            return new[] { paid1, paid2, paid3 };
        }

        // ════════════════════════════════════════════════════════════════════════
        // CANCELLED INVOICES
        // ════════════════════════════════════════════════════════════════════════

        private IEnumerable<Invoice> BuildCancelledInvoices()
        {
            // Cancelled from DRAFT
            var cancelled1 = new Invoice(
                invoiceNumber: "INV-2025-0009",
                invoiceDate: new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc),
                dueDate: new DateTime(2025, 2, 10, 0, 0, 0, DateTimeKind.Utc),
                clientId: ClientAlphaId,
                clientFullName: "Alpha Tech SARL",
                clientAddress: "12 Avenue de la République, Tunis 1001",
                additionalNotes: "Cancelled – client changed requirements before finalization.");

            cancelled1.AddItem(new InvoiceItem(
                cancelled1.Id, ArticleWebcamId,
                "HD Webcam 1080p", "WC-1080P",
                quantity: 3, uniPriceHT: 75m, taxRate: 0.19m));

            cancelled1.CancelInvoice(); // allowed from DRAFT

            // Cancelled from UNPAID
            var cancelled2 = new Invoice(
                invoiceNumber: "INV-2025-0014",
                invoiceDate: new DateTime(2025, 1, 20, 0, 0, 0, DateTimeKind.Utc),
                dueDate: new DateTime(2025, 2, 20, 0, 0, 0, DateTimeKind.Utc),
                clientId: ClientGammaId,
                clientFullName: "Gamma Consulting Group",
                clientAddress: "8 Rue de Marseille, Sousse 4000",
                additionalNotes: "Cancelled – order dispute with client.");

            cancelled2.AddItem(new InvoiceItem(
                cancelled2.Id, ArticleKeyboardId,
                "Mechanical Keyboard TKL", "KB-MECH-TKL",
                quantity: 6, uniPriceHT: 85m, taxRate: 0.19m));

            cancelled2.FinalizeInvoice();
            cancelled2.CancelInvoice(); // allowed from UNPAID

            return new[] { cancelled1, cancelled2 };
        }

        // ════════════════════════════════════════════════════════════════════════
        // SOFT-DELETED INVOICES  (IsDeleted = true)
        // ════════════════════════════════════════════════════════════════════════

        private IEnumerable<Invoice> BuildDeletedInvoices()
        {
            // Deleted draft – entered by mistake
            var deleted1 = new Invoice(
                invoiceNumber: "INV-2025-VOID-001",
                invoiceDate: new DateTime(2025, 4, 5, 0, 0, 0, DateTimeKind.Utc),
                dueDate: new DateTime(2025, 5, 5, 0, 0, 0, DateTimeKind.Utc),
                clientId: ClientBetaId,
                clientFullName: "Beta Solutions Ltd.",
                clientAddress: "55 Rue Ibn Khaldoun, Sfax 3000",
                additionalNotes: "Created by mistake – soft deleted.");

            deleted1.AddItem(new InvoiceItem(
                deleted1.Id, ArticleMouseId,
                "Wireless Ergonomic Mouse", "MSE-ERG-WL",
                quantity: 1, uniPriceHT: 55m, taxRate: 0.19m));

            deleted1.Delete();

            // Deleted paid invoice – archived after period close
            var deleted2 = new Invoice(
                invoiceNumber: "INV-2024-0112",
                invoiceDate: new DateTime(2024, 12, 1, 0, 0, 0, DateTimeKind.Utc),
                dueDate: new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                clientId: ClientAlphaId,
                clientFullName: "Alpha Tech SARL",
                clientAddress: "12 Avenue de la République, Tunis 1001",
                additionalNotes: "FY-2024 archived invoice.");

            deleted2.AddItem(new InvoiceItem(
                deleted2.Id, ArticleLaptopId,
                "Laptop Pro 15\" 32GB", "LP15-32GB",
                quantity: 3, uniPriceHT: 1_800m, taxRate: 0.19m));

            deleted2.FinalizeInvoice();
            deleted2.MarkAsPaid();
            deleted2.Delete(); // archive after fiscal year close

            return new[] { deleted1, deleted2 };
        }
    }
}