using InvoiceService.Application.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace InvoiceService.Services;

public class InvoicePdfGenerator : IInvoicePdfGenerator
{
    private const string CurrencySymbol = "TND";
    private const string CompanyName = "YOUR COMPANY";
    private const string CompanyAddress = "123 Business Ave, City, Country";
    private const string CompanyEmail = "contact@company.com";
    private const string CompanyPhone = "+216 71 123 456";

    public byte[] GenerateInvoicePdf(InvoiceDto invoice)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var statusColor = invoice.Status switch
        {
            "PAID" => Colors.Green.Medium,
            "UNPAID" => Colors.Orange.Medium,
            "CANCELLED" => Colors.Red.Medium,
            _ => Colors.Grey.Medium
        };

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                // ================= HEADER =================
                page.Header().Row(row =>
                {
                    // Left side: Logo + Company details
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text(CompanyName)
                            .FontSize(22).Bold().FontColor(Colors.Blue.Darken2);

                        col.Item().Text(CompanyAddress).FontSize(9);
                        col.Item().Text($"Tel: {CompanyPhone} | Email: {CompanyEmail}").FontSize(9);
                    });

                    // Right side: Invoice title + number + status
                    row.ConstantItem(220).AlignRight().Column(col =>
                    {
                        col.Item().Text("INVOICE")
                            .FontSize(24).Bold().FontColor(Colors.Blue.Darken2);

                        col.Item().Text(invoice.InvoiceNumber)
                            .FontSize(14).SemiBold();

                        col.Item().Background(statusColor).PaddingVertical(5).PaddingHorizontal(10)
                            .AlignCenter()
                            .Text(invoice.Status).FontColor(Colors.White).Bold().FontSize(11);
                    });
                });

                // ================= CONTENT =================
                page.Content().PaddingVertical(20).Column(col =>
                {
                    // CLIENT + DATES
                    col.Item().Row(row =>
                    {
                        // Bill To
                        row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(12).Column(c =>
                        {
                            c.Item().Text("BILL TO").Bold().FontSize(11);
                            c.Item().Height(5);
                            c.Item().Text(invoice.ClientFullName).FontSize(10);
                            c.Item().Text(invoice.ClientAddress).FontSize(10);
                        });

                        // Invoice Dates
                        row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(12).Column(c =>
                        {
                            c.Item().Text("INVOICE DETAILS").Bold().FontSize(11);
                            c.Item().Height(5);
                            c.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Invoice Date:").Bold();
                                r.ConstantItem(100).AlignRight().Text($"{invoice.InvoiceDate:dd/MM/yyyy}");
                            });
                            c.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Due Date:").Bold();
                                r.ConstantItem(100).AlignRight().Text($"{invoice.DueDate:dd/MM/yyyy}");
                            });
                        });
                    });

                    col.Item().PaddingVertical(10);

                    // ================= TABLE =================
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1.5f);
                        });

                        // Header
                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(8).Text("Article").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(8).Text("Qty").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(8).Text("Unit Price (HT)").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(8).Text("TVA").Bold().AlignCenter();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(8).Text("Total TTC").Bold().AlignRight();
                        });

                        // Rows
                        var index = 0;
                        foreach (var item in invoice.Items)
                        {
                            var bgColor = index++ % 2 == 0 ? Colors.White : Colors.Grey.Lighten3;
                            table.Cell().Background(bgColor).Padding(6).Text(item.ArticleName);
                            table.Cell().Background(bgColor).Padding(6).Text($"{item.Quantity:N2}").AlignRight();
                            table.Cell().Background(bgColor).Padding(6).Text($"{item.UniPriceHT:N2} {CurrencySymbol}").AlignRight();
                            table.Cell().Background(bgColor).Padding(6).AlignCenter().Text($"{item.TaxRate * 100:F0}%");
                            table.Cell().Background(bgColor).Padding(6).Text($"{item.TotalTTC:N2} {CurrencySymbol}").AlignRight();
                        }
                    });

                    col.Item().PaddingVertical(15);

                    // ================= TOTALS =================
                    // ================= TOTALS =================
                    col.Item().AlignRight().Width(240).Border(1).BorderColor(Colors.Grey.Lighten1).Padding(12).Column(totals =>
                    {
                        totals.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Subtotal (HT):").Bold();
                            r.ConstantItem(100).AlignRight().Text($"{invoice.TotalHT:N2} {CurrencySymbol}");
                        });

                        // TVA breakdown depends on mode
                        if (invoice.TaxMode == TaxCalculationMode.INVOICE)
                        {
                            // Single blended rate line
                            var effectiveRate = invoice.TotalHT > 0
                                ? (invoice.TotalTVA / invoice.TotalHT) * 100
                                : 0;

                            totals.Item().Row(r =>
                            {
                                r.RelativeItem().Text($"TVA ({effectiveRate:F2}%):").Bold();
                                r.ConstantItem(100).AlignRight().Text($"{invoice.TotalTVA:N2} {CurrencySymbol}");
                            });
                        }
                        else
                        {
                            // Per-rate breakdown (LINE mode)
                            var taxGroups = invoice.Items
                                .GroupBy(i => i.TaxRate)
                                .Select(g => new
                                {
                                    Rate = g.Key * 100,
                                    Amount = g.Sum(i => Math.Round(i.TotalHT * i.TaxRate, 2))
                                })
                                .OrderBy(g => g.Rate);

                            foreach (var group in taxGroups)
                            {
                                totals.Item().Row(r =>
                                {
                                    r.RelativeItem().Text($"TVA ({group.Rate:F0}%):").Bold();
                                    r.ConstantItem(100).AlignRight().Text($"{group.Amount:N2} {CurrencySymbol}");
                                });
                            }
                        }

                        totals.Item().LineHorizontal(1).LineColor(Colors.Grey.Medium);
                        totals.Item().Row(r =>
                        {
                            r.RelativeItem().Text("TOTAL TTC:").Bold().FontSize(12);
                            r.ConstantItem(100).AlignRight().Text($"{invoice.TotalTTC:N2} {CurrencySymbol}").Bold().FontSize(12);
                        });
                    });

                    // ================= NOTES =================
                    if (!string.IsNullOrWhiteSpace(invoice.AdditionalNotes))
                    {
                        col.Item().PaddingTop(20).Border(1).BorderColor(Colors.Grey.Lighten1).Padding(10).Column(c =>
                        {
                            c.Item().Text("NOTES").Bold().FontSize(11);
                            c.Item().Height(5);
                            c.Item().Text(invoice.AdditionalNotes).FontSize(9);
                        });
                    }

                    // ================= PAYMENT TERMS =================
                    col.Item().PaddingTop(20).Column(c =>
                    {
                        c.Item().Text("Payment Terms").Bold().FontSize(10);
                        c.Item().Text("Please pay within the due date. Bank transfer details available upon request.")
                            .FontSize(9).FontColor(Colors.Grey.Darken1);
                    });
                });

                // ================= FOOTER =================
                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ").FontSize(9).FontColor(Colors.Grey.Medium);
                    x.CurrentPageNumber().FontSize(9).FontColor(Colors.Grey.Medium);
                    x.Span(" - Thank you for your business").FontSize(9).FontColor(Colors.Grey.Medium);
                });
            });
        });

        return document.GeneratePdf();
    }
}

public interface IInvoicePdfGenerator
{
    byte[] GenerateInvoicePdf(InvoiceDto invoice);
}