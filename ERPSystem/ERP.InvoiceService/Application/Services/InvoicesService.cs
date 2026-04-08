using ERP.InvoiceService.Infrastructure.Messaging;
using ERP.InvoiceService.Infrastructure.Persistence;
using InvoiceService.Application.DTOs;
using InvoiceService.Application.Exceptions;
using InvoiceService.Application.Interfaces;
using InvoiceService.Domain;

namespace InvoiceService.Application.Services
{
    public class InvoicesService : IInvoicesService
    {
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IClientServiceHttpClient _clientServiceHttpClient;
        private readonly IArticleServiceHttpClient _articleServiceHttpClient;
        private readonly IInvoiceNumberGenerator _invoiceNumberGenerator;

        public InvoicesService(
            IInvoiceRepository invoiceRepository,
            IInvoiceNumberGenerator invoiceNumberGenerator,
            IClientServiceHttpClient clientServiceHttpClient,
            IArticleServiceHttpClient articleServiceHttpClient) // Add missing dependencies
        {
            _invoiceRepository = invoiceRepository;
            _invoiceNumberGenerator = invoiceNumberGenerator; // THIS WAS MISSING!
            _clientServiceHttpClient = clientServiceHttpClient;
            _articleServiceHttpClient = articleServiceHttpClient;
        }

        public async Task<InvoiceDto> GetByIdAsync(Guid id)
        {
            var invoice = await _invoiceRepository.GetByIdWithItemsAsync(id)
                ?? throw new InvoiceNotFoundException(id);

            return invoice.ToDto();
        }
        public async Task<IEnumerable<InvoiceDto>> GetAllAsync(bool includeDeleted = false)
        {
            var invoices = await _invoiceRepository.GetAllAsync(includeDeleted);
            return invoices.Select(i => i.ToDto());
        }
        public async Task<IEnumerable<InvoiceDto>> GetByClientIdAsync(Guid clientId)
        {
            var invoices = await _invoiceRepository.GetByClientIdAsync(clientId);
            return invoices.Select(i => i.ToDto());
        }
        public async Task<IEnumerable<InvoiceDto>> GetByStatusAsync(InvoiceStatus status)
        {
            var invoices = await _invoiceRepository.GetByStatusAsync(status);
            return invoices.Select(i => i.ToDto());
        }

        // ════════════════════════════════════════════════════════════════════════════
        // CREATE OPERATIONS
        // ════════════════════════════════════════════════════════════════════════════

        public async Task<InvoiceDto> CreateAsync(CreateInvoiceDto dto)
        {
            var client = await _clientServiceHttpClient.GetByIdAsync(dto.ClientId);

            decimal invoiceTotalTTC = dto.Items.Sum(i => i.Quantity * i.UniPriceHT * (1 + i.TaxRate));

            await CheckClientCreditLimit(client.Id, invoiceTotalTTC);

            var invoiceNumber = await _invoiceNumberGenerator.GenerateNextInvoiceNumberAsync();

            // add dropdown to list available clients and select it 

            // ──── CREATE INVOICE ────
            var invoice = new Invoice(
                invoiceNumber,
                dto.InvoiceDate,
                dto.DueDate,
                dto.ClientId,
                client.Name,
                client.Address,
                dto.AdditionalNotes);


            foreach (var itemDto in dto.Items)
            {
                var article = await _articleServiceHttpClient.GetByIdAsync(itemDto.ArticleId);

                var item = new InvoiceItem(
                    invoice.Id,
                    itemDto.ArticleId,
                    article.Libelle,
                    article.BarCode,
                    itemDto.Quantity,
                    itemDto.UniPriceHT,
                    itemDto.TaxRate);

                invoice.AddItem(item);
            }


            await _invoiceRepository.AddAsync(invoice);
            return invoice.ToDto();
        }

        public async Task<InvoiceDto> AddItemAsync(Guid invoiceId, AddInvoiceItemDto dto)
        {

            var invoice = await _invoiceRepository.GetByIdWithItemsAsync(invoiceId)
                ?? throw new InvoiceNotFoundException(invoiceId);

            var article = _articleServiceHttpClient.GetByIdAsync(dto.ArticleId).Result;


            var client = await _clientServiceHttpClient.GetByIdAsync(invoice.ClientId);
            var itemTotalTTC = dto.Quantity * dto.UniPriceHT * (1 + dto.TaxRate);

            await CheckClientCreditLimit(client.Id, itemTotalTTC);

            var item = new InvoiceItem(
                invoiceId,
                dto.ArticleId,
                article.Libelle,
                article.BarCode,
                dto.Quantity,
                dto.UniPriceHT,
                dto.TaxRate);

            invoice.AddItem(item);


            await _invoiceRepository.UpdateAsync(invoice);
            return invoice.ToDto();
        }


        public async Task RemoveItemAsync(Guid invoiceId, Guid itemId)
        {

            var invoice = await _invoiceRepository.GetByIdWithItemsAsync(invoiceId)
                ?? throw new InvoiceNotFoundException(invoiceId);
            invoice.RemoveItem(itemId);

            await _invoiceRepository.UpdateAsync(invoice);
        }

        public async Task<InvoiceDto> FinalizeAsync(Guid id)
        {

            var invoice = await _invoiceRepository.GetByIdWithItemsAsync(id)
                ?? throw new InvoiceNotFoundException(id);
            invoice.FinalizeInvoice();

            await _invoiceRepository.UpdateAsync(invoice);
            return invoice.ToDto();
        }
        public async Task<InvoiceDto> MarkAsPaidAsync(Guid id)
        {
            // ──── GET INVOICE ────
            var invoice = await _invoiceRepository.GetByIdAsync(id)
                ?? throw new InvoiceNotFoundException(id);
            invoice.MarkAsPaid();
            await _invoiceRepository.UpdateAsync(invoice);
            return invoice.ToDto();
        }
        public async Task<InvoiceDto> CancelAsync(Guid id)
        {
            var invoice = await _invoiceRepository.GetByIdAsync(id)
                ?? throw new InvoiceNotFoundException(id);
            invoice.CancelInvoice();

            await _invoiceRepository.UpdateAsync(invoice);
            return invoice.ToDto();
        }

        // ════════════════════════════════════════════════════════════════════════════
        // SOFT DELETE OPERATIONS
        // ════════════════════════════════════════════════════════════════════════════
        public async Task DeleteAsync(Guid id)
        {
            // ──── GET INVOICE ────
            var invoice = await _invoiceRepository.GetByIdAsync(id)
                ?? throw new InvoiceNotFoundException(id);

            invoice.Delete();

            // ──── PERSIST ────
            await _invoiceRepository.UpdateAsync(invoice);
        }
        public async Task RestoreAsync(Guid id)
        {
            var invoice = await _invoiceRepository.GetByIdDeletedAsync(id) ?? throw new InvoiceNotFoundException(id);

            // ──── RESTORE ────
            invoice.Restore();

            // ──── PERSIST ────
            await _invoiceRepository.UpdateAsync(invoice);
        }


        // =========================
        // STATS
        // =========================
        public async Task<InvoiceStatsDto> GetStatsAsync(int topClientsCount = 5)
        {
            var projections = (await _invoiceRepository.GetStatsProjectionAsync()).ToList();
            var deletedCount = await _invoiceRepository.GetDeletedCountAsync();

            var now = DateTime.UtcNow;

            // ── Status buckets ───────────────────────────────────────────────────────
            var drafts = projections.Where(i => i.Status == InvoiceStatus.DRAFT).ToList();
            var unpaid = projections.Where(i => i.Status == InvoiceStatus.UNPAID).ToList();
            var paid = projections.Where(i => i.Status == InvoiceStatus.PAID).ToList();
            var cancelled = projections.Where(i => i.Status == InvoiceStatus.CANCELLED).ToList();
            var overdue = unpaid.Where(i => i.DueDate < now).ToList();

            // ── Revenue (PAID only) ──────────────────────────────────────────────────
            var revenueHT = paid.Sum(i => i.TotalHT);
            var revenueTTC = paid.Sum(i => i.TotalTTC);
            var tvaColl = paid.Sum(i => i.TotalTVA);

            // ── Outstanding (UNPAID) ─────────────────────────────────────────────────
            var outstandingHT = unpaid.Sum(i => i.TotalHT);
            var outstandingTTC = unpaid.Sum(i => i.TotalTTC);

            // ── Overdue ──────────────────────────────────────────────────────────────
            var overdueHT = overdue.Sum(i => i.TotalHT);
            var overdueTTC = overdue.Sum(i => i.TotalTTC);

            // ── Average invoice value (PAID + UNPAID, i.e. real commercial invoices) ─
            var activeInvoices = paid.Concat(unpaid).ToList();
            var avgValueHT = activeInvoices.Count > 0
                ? activeInvoices.Average(i => i.TotalHT)
                : 0m;

            // ── Average days to due (proxy for payment cycle) — PAID invoices only ───
            var avgPaymentDays = paid.Count > 0
                ? paid.Average(i => (i.DueDate - i.InvoiceDate).TotalDays)
                : 0d;

            // ── Top clients by paid revenue TTC ─────────────────────────────────────
            var topClients = paid
                .GroupBy(i => new { i.ClientId, i.ClientFullName })
                .Select(g => new ClientRevenueDto
                (
                    ClientId: g.Key.ClientId,
                    ClientFullName: g.Key.ClientFullName,
                    InvoiceCount: g.Count(),
                    RevenueTTC: g.Sum(i => i.TotalTTC)
                ))
                .OrderByDescending(c => c.RevenueTTC)
                .Take(topClientsCount)
                .ToList();

            // ── Monthly breakdown (current calendar year) ────────────────────────────
            var currentYear = now.Year;
            var yearInvoices = projections
                .Where(i => i.InvoiceDate.Year == currentYear)
                .ToList();

            var monthlyBreakdown = Enumerable.Range(1, 12)
                .Select(month =>
                {
                    var issued = yearInvoices
                        .Where(i => i.InvoiceDate.Month == month)
                        .ToList();

                    var monthPaid = issued
                        .Where(i => i.Status == InvoiceStatus.PAID)
                        .ToList();

                    return new MonthlyStatsDto
                    (
                        Year: currentYear,
                        Month: month,
                        IssuedCount: issued.Count,
                        PaidCount: monthPaid.Count,
                        IssuedTTC: issued.Sum(i => i.TotalTTC),
                        PaidTTC: monthPaid.Sum(i => i.TotalTTC)
                    );
                })
                .ToList();

            // ── Assemble ─────────────────────────────────────────────────────────────
            return new InvoiceStatsDto
            (
                TotalInvoices: projections.Count,
                DraftCount: drafts.Count,
                UnpaidCount: unpaid.Count,
                PaidCount: paid.Count,
                CancelledCount: cancelled.Count,
                DeletedCount: deletedCount,
                OverdueCount: overdue.Count,

                TotalRevenueHT: revenueHT,
                TotalRevenueTTC: revenueTTC,
                TotalTVACollected: tvaColl,

                OutstandingHT: outstandingHT,
                OutstandingTTC: outstandingTTC,

                OverdueHT: overdueHT,
                OverdueTTC: overdueTTC,

                AverageInvoiceValueHT: avgValueHT,
                AveragePaymentDays: avgPaymentDays,

                TopClients: topClients,
                MonthlyBreakdown: monthlyBreakdown
            );
        }

        private async Task CheckClientCreditLimit(Guid clientId, decimal invoiceTotalTTC)
        {
            var client = await _clientServiceHttpClient.GetByIdAsync(clientId);
            var invoices = await _invoiceRepository.GetByClientIdAsync(clientId);

            // Current outstanding total for the client
            var clientCurrentCredit = invoices
                        .Where(i => i.Status == InvoiceStatus.UNPAID)
                        .Sum(i => i.TotalTTC);

            // Check credit limit
            if (client.CreditLimit < invoiceTotalTTC + clientCurrentCredit)
                throw new InvoiceDomainException(
                    $"Cannot create invoice. Client '{client.Name}' exceeds credit limit." +
                    $" Current used: {clientCurrentCredit:C}, Attempted invoice: {invoiceTotalTTC:C}, Limit: {client.CreditLimit:C}"
                );
        }

    }
}