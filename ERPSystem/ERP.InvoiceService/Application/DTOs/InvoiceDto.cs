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

/// <summary>
/// Aggregated statistics snapshot for the invoice service.
/// </summary>
public class InvoiceStatsDto
{
    // ── Volume ───────────────────────────────────────────────────────────────
    public int TotalInvoices { get; init; }
    public int DraftCount { get; init; }
    public int UnpaidCount { get; init; }
    public int PaidCount { get; init; }
    public int CancelledCount { get; init; }
    public int DeletedCount { get; init; }
    public int OverdueCount { get; init; }   // UNPAID + past DueDate

    // ── Revenue ──────────────────────────────────────────────────────────────
    public decimal TotalRevenueHT { get; init; }   // PAID invoices, excl. tax
    public decimal TotalRevenueTTC { get; init; }   // PAID invoices, incl. tax
    public decimal TotalTVACollected { get; init; }   // PAID invoices, tax only

    public decimal OutstandingHT { get; init; }   // UNPAID, excl. tax
    public decimal OutstandingTTC { get; init; }   // UNPAID, incl. tax

    public decimal OverdueHT { get; init; }   // UNPAID + overdue, excl. tax
    public decimal OverdueTTC { get; init; }   // UNPAID + overdue, incl. tax

    // ── Averages ─────────────────────────────────────────────────────────────
    public decimal AverageInvoiceValueHT { get; init; }  // across PAID + UNPAID
    public double AveragePaymentDays { get; init; }  // PAID only (DueDate span)

    // ── Top clients (by paid revenue TTC) ───────────────────────────────────
    public IReadOnlyList<ClientRevenueDto> TopClients { get; init; } = [];

    // ── Monthly breakdown (current calendar year) ───────────────────────────
    public IReadOnlyList<MonthlyStatsDto> MonthlyBreakdown { get; init; } = [];
}

public class ClientRevenueDto
{
    public Guid ClientId { get; init; }
    public string ClientFullName { get; init; } = string.Empty;
    public int InvoiceCount { get; init; }
    public decimal RevenueTTC { get; init; }
}

public class MonthlyStatsDto
{
    public int Year { get; init; }
    public int Month { get; init; }
    public int IssuedCount { get; init; }
    public int PaidCount { get; init; }
    public decimal IssuedTTC { get; init; }
    public decimal PaidTTC { get; init; }
}