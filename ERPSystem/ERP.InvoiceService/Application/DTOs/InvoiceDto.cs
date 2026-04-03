using System;
using System.Collections.Generic;
using InvoiceService.Domain;

namespace InvoiceService.Application.DTOs
{
    public class InvoiceDto
    {
        public Guid Id { get; set; }
        public string InvoiceNumber { get; set; } = default!;
        public DateTime InvoiceDate { get; set; }
        public DateTime DueDate { get; set; }
        public decimal TotalHT { get; set; }
        public decimal TotalTVA { get; set; }
        public decimal TotalTTC { get; set; }
        public string Status { get; set; } = default!; 
        public Guid ClientId { get; set; }
        public string ClientFullName { get; set; } = default!;
        public string ClientAddress { get; set; } = default!;
        public string? AdditionalNotes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public List<InvoiceItemDto> Items { get; set; } = new();
    }
    public class InvoiceItemDto
    {
        public Guid Id { get; set; }
        public Guid ArticleId { get; set; }
        public string ArticleName { get; set; } = default!;
        public string ArticleBarCode { get; set; } = default!;
        public int Quantity { get; set; }
        public decimal UniPriceHT { get; set; }
        public decimal TaxRate { get; set; }
        public decimal TotalHT { get; set; }
        public decimal TotalTTC { get; set; }
    }

    // ════════════════════════════════════════════════════════════════════════════
    // CREATE / COMMAND DTOs - Used to create new invoices
    // ════════════════════════════════════════════════════════════════════════════
    public class CreateInvoiceDto
    {
        public string InvoiceNumber { get; set; } = default!;
        public DateTime InvoiceDate { get; set; }
        public DateTime DueDate { get; set; }
        public Guid ClientId { get; set; }
        public string ClientFullName { get; set; } = default!;
        public string ClientAddress { get; set; } = default!;
        public string? AdditionalNotes { get; set; }
        public List<CreateInvoiceItemDto> Items { get; set; } = new();
    }
    public class CreateInvoiceItemDto
    {
        public Guid ArticleId { get; set; }
        public string ArticleName { get; set; } = default!;
        public string ArticleBarCode { get; set; } = default!;
        public int Quantity { get; set; }
        public decimal UniPriceHT { get; set; }
        public decimal TaxRate { get; set; }
    }
    public class AddInvoiceItemDto
    {
        public Guid ArticleId { get; set; }
        public string ArticleName { get; set; } = default!;
        public string ArticleBarCode { get; set; } = default!;
        public int Quantity { get; set; }
        public decimal UniPriceHT { get; set; }
        public decimal TaxRate { get; set; }
    }
}