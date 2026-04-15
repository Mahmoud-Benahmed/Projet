using InvoiceService.Domain;

namespace InvoiceService.Application.DTOs
{

    public static class InvoiceMapping
    {
        public static InvoiceDto ToDto(this Invoice invoice)
        {
            return new InvoiceDto
            (
                Id: invoice.Id,
                InvoiceNumber: invoice.InvoiceNumber,
                InvoiceDate: invoice.InvoiceDate,
                DueDate: invoice.DueDate,
                TotalHT: invoice.TotalHT,
                TotalTVA: invoice.TotalTVA,
                TotalTTC: invoice.TotalTTC,
                Status: invoice.Status.ToString(),
                ClientId: invoice.ClientId,
                ClientFullName: invoice.ClientFullName,
                ClientAddress: invoice.ClientAddress,
                AdditionalNotes: invoice.AdditionalNotes,
                IsDeleted: invoice.IsDeleted,
                CreatedAt: invoice.CreatedAt,
                UpdatedAt: invoice.UpdatedAt,
                Items: invoice.Items.Select(i => i.ToDto()).ToList()
            );
        }
        public static InvoiceItemDto ToDto(this InvoiceItem item)
        {
            return new InvoiceItemDto
            (
                Id: item.Id,
                ArticleId: item.ArticleId,
                ArticleName: item.ArticleName,
                ArticleBarCode: item.ArticleBarCode,
                Quantity: item.Quantity,
                UniPriceHT: item.UniPriceHT,
                TaxRate: item.TaxRate,
                TotalHT: item.TotalHT,
                TotalTTC: item.TotalTTC
            );
        }
    }
}