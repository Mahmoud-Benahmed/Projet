using ERP.InvoiceService.Application.Interfaces;
using ERP.InvoiceService.Infrastructure.Messaging;
using ERP.InvoiceService.Infrastructure.Messaging.Events;
using ERP.InvoiceService.Infrastructure.Persistence;
using InvoiceService.Application.DTOs;
using InvoiceService.Application.Exceptions;
using InvoiceService.Application.Interfaces;
using InvoiceService.Domain;

namespace ERP.InvoiceService.Application.Services.LocalCache.ArticleCache
{
    public class InvoicesService : IInvoicesService
    {
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IInvoiceNumberGenerator _invoiceNumberGenerator;
        private readonly IClientCacheRepository _clientCacheRepository;
        private readonly IArticleCacheRepository _articleCacheRepository;
        private readonly IEventPublisher _eventPublisher;
        private readonly IStockServiceHttpClient _stockClient;


        public InvoicesService(
            IStockServiceHttpClient stockClient,
            IInvoiceRepository invoiceRepository,
            IInvoiceNumberGenerator invoiceNumberGenerator,
            IClientCacheRepository clientCacheRepository,
            IArticleCacheRepository articleCacheRepository,
            IEventPublisher eventPublisher)
        {
            _stockClient = stockClient;
            _eventPublisher= eventPublisher;
            _clientCacheRepository = clientCacheRepository;
            _invoiceRepository = invoiceRepository;
            _invoiceNumberGenerator = invoiceNumberGenerator;
            _articleCacheRepository = articleCacheRepository;
        }

        public async Task<InvoiceDto> GetByIdAsync(Guid id)
        {
            var invoice = await _invoiceRepository.GetByIdWithItemsAsync(id)
                ?? throw new InvoiceNotFoundException(id);

            return invoice.ToDto();
        }
        public async Task<List<InvoiceDto>> GetAllAsync(bool includeDeleted = false)
        {
            var invoices = await _invoiceRepository.GetAllAsync(includeDeleted);
            return invoices.Select(i => i.ToDto()).ToList();
        }
        public async Task<List<InvoiceDto>> GetByClientIdAsync(Guid clientId)
        {
            var invoices = await _invoiceRepository.GetByClientIdAsync(clientId);
            return invoices.Select(i => i.ToDto()).ToList();
        }
        public async Task<List<InvoiceDto>> GetByStatusAsync(InvoiceStatus status)
        {
            var invoices = await _invoiceRepository.GetByStatusAsync(status);
            return invoices.Select(i => i.ToDto()).ToList();
        }

        // ════════════════════════════════════════════════════════════════════════════
        // CREATE OPERATIONS
        // ════════════════════════════════════════════════════════════════════════════

        public async Task<InvoiceDto> CreateAsync(CreateInvoiceDto dto)
        {
            if (dto.Items == null || !dto.Items.Any())
                throw new InvoiceDomainException("Invoice must have at least one item.");

            if (dto.InvoiceDate > dto.DueDate)
                throw new InvoiceDomainException("Due date cannot be before invoice date.");

            var client = await _clientCacheRepository.GetByIdAsync(dto.ClientId) ?? throw new KeyNotFoundException($"Client with Id {dto.ClientId} not found.");

            var stockStatus = await _stockClient.GetStockStatusAsync();


            ValidateStockAvailability(dto.Items, stockStatus);


            decimal invoiceTotalTTC = dto.Items.Sum(i => i.Quantity * i.UniPriceHT * (1 + (i.TaxRate)));

            await CheckClientCreditLimit(client.Id, invoiceTotalTTC);

            var invoiceNumber = await _invoiceNumberGenerator.GenerateNextInvoiceNumberAsync();

            // add dropdown to list available clients and select it 

            // ──── CREATE INVOICE ────
            var invoice = new Invoice(
                invoiceNumber,
                dto.InvoiceDate,
                dto.DueDate,
                client.Id,
                client.Name,
                client.Address,
                dto.AdditionalNotes);


            // Load all articles once (single database call)
            var articleIds = dto.Items.Select(i => i.ArticleId).Distinct().ToList();
            var articles = await _articleCacheRepository.GetByIdsAsync(articleIds);
            if (articles == null || !articles.Any())
                throw new InvalidOperationException("No articles found for the given IDs.");

            var articleDictionary = articles.ToDictionary(a => a.Id, a => a);

            // Now loop through items - no database calls here!
            foreach (var itemDto in dto.Items)
            {
                if (articleDictionary.TryGetValue(itemDto.ArticleId, out var article))
                {
                    invoice.AddItem(new InvoiceItem(
                        invoice.Id,
                        itemDto.ArticleId,
                        article.Libelle,
                        article.BarCode,
                        itemDto.Quantity,
                        itemDto.UniPriceHT,
                        itemDto.TaxRate));
                }
                else
                {
                    throw new InvalidOperationException($"Article with ID {itemDto.ArticleId} not found");
                }
            }


            await _invoiceRepository.AddAsync(invoice);

            var payload = invoice.ToDto(); 
            
            if (invoice.Status == InvoiceStatus.UNPAID)
                await _eventPublisher.PublishAsync(InvoiceTopics.Created, payload);

            return payload;
        }

        public async Task<InvoiceDto> UpdateAsync(Guid id, UpdateInvoiceDto dto)
        {
            var invoice = await _invoiceRepository.GetByIdWithItemsAsync(id)
                ?? throw new InvoiceNotFoundException(id);

            var client = await _clientCacheRepository.GetByIdAsync(dto.ClientId) ?? throw new KeyNotFoundException($"Client with Id {dto.ClientId} not found.");
            
            if (invoice.Status != InvoiceStatus.DRAFT)
                throw new InvoiceDomainException("Only DRAFT invoices can be updated.");

            decimal invoiceTotalTTC = dto.Items.Sum(i => i.Quantity * i.UniPriceHT * (1 + (i.TaxRate)));

            await CheckClientCreditLimit(dto.ClientId, invoiceTotalTTC, excludeInvoiceId: id);

            // Update header only
            invoice.Update(
                invoiceDate: dto.InvoiceDate,
                dueDate: dto.DueDate,
                clientId: dto.ClientId,
                clientFullName: client.Name,
                clientAddress: client.Address,
                additionalNotes: dto.AdditionalNotes
            );


            // Load all articles once (single database call)
            var articleIds = dto.Items.Select(i => i.ArticleId).Distinct().ToList();
            var articles = await _articleCacheRepository.GetByIdsAsync(articleIds);
            if (articles == null || !articles.Any())
                throw new InvalidOperationException("No articles found for the given IDs.");

            var articleDictionary = articles.ToDictionary(a => a.Id, a => a);

            invoice.ClearItems();
            foreach (var itemDto in dto.Items)
            {
                if (articleDictionary.TryGetValue(itemDto.ArticleId, out var article))
                {
                    invoice.AddItem(new InvoiceItem(
                        invoice.Id,
                        itemDto.ArticleId,
                        article.Libelle,
                        article.BarCode,
                        itemDto.Quantity,
                        itemDto.UniPriceHT,
                        itemDto.TaxRate));
                }
                else
                {
                    // Handle missing article - log, throw, or skip
                    throw new InvalidOperationException($"Article with ID {itemDto.ArticleId} not found");
                }
            }

            await _invoiceRepository.UpdateAsync(invoice);
            return invoice.ToDto();
        }

        public async Task<InvoiceDto> AddItemAsync(Guid invoiceId, AddInvoiceItemDto dto)
        {

            var invoice = await _invoiceRepository.GetByIdWithItemsAsync(invoiceId)
                ?? throw new InvoiceNotFoundException(invoiceId);
                
            if (invoice.Status != InvoiceStatus.DRAFT)
                throw new InvoiceDomainException("Items can only be added to DRAFT invoices.");

            var invoiceTotalTTC = invoice.TotalTTC + (dto.Quantity * dto.UniPriceHT * (1 + (dto.TaxRate)));
            
            await CheckClientCreditLimit(invoice.ClientId, invoiceTotalTTC);

            var article = await _articleCacheRepository.GetByIdAsync(dto.ArticleId) ?? throw new KeyNotFoundException($"Article with Id: {dto.ArticleId} not found.");

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

            if (invoice.Status != InvoiceStatus.DRAFT)
                throw new InvoiceDomainException("Items can only be removed from DRAFT invoices.");


            invoice.RemoveItem(itemId);

            await _invoiceRepository.UpdateAsync(invoice);
        }

        public async Task<InvoiceDto> FinalizeAsync(Guid id)
        {

            var invoice = await _invoiceRepository.GetByIdWithItemsAsync(id)
                ?? throw new InvoiceNotFoundException(id);
            
            ValidateStockAvailability(
                invoice.Items.Select(i => new CreateInvoiceItemDto
                (
                    ArticleId: i.ArticleId,
                    Quantity: i.Quantity,
                    UniPriceHT: i.UniPriceHT,
                    TaxRate: i.TaxRate
                )).ToList(), 
                await _stockClient.GetStockStatusAsync());

            invoice.FinalizeInvoice();


            await _invoiceRepository.UpdateAsync(invoice);

            var payload = invoice.ToDto();

            // Draft invoices are not published when created with InvoiceStatus.DRAFT in CreateAsync (method above),
            // They are considered created once their Status is UNPAID Meaning that the stock will be effected only by the UNPAID invoices not the DRAFT ones,
            // the draft invoice is peristed in Invoice service as Draft but only sent as UNPAID to StockService so the DRAFT isn't published in CreateAsync,
            // they are published once they are UNPAID so they will be persisted in the StockService for tracking by this statement.
            if (invoice.Status == InvoiceStatus.UNPAID)
                await _eventPublisher.PublishAsync(InvoiceTopics.Created, payload); 

            return payload;
        }
        public async Task<InvoiceDto> MarkAsPaidAsync(Guid id)
        {
            // ──── GET INVOICE ────
            var invoice = await _invoiceRepository.GetByIdAsync(id)
                ?? throw new InvoiceNotFoundException(id);
            invoice.MarkAsPaid();
            await _invoiceRepository.UpdateAsync(invoice);

            // publish event to payment service
            //await _eventPublisher.PublishAsync(PaymentTopics.PAID, payload);

            return invoice.ToDto();
        }
        public async Task<InvoiceDto> CancelAsync(Guid id)
        {
            var invoice = await _invoiceRepository.GetByIdWithItemsAsync(id)
                ?? throw new InvoiceNotFoundException(id);

            invoice.CancelInvoice();

            await _invoiceRepository.UpdateAsync(invoice);

            var payload= invoice.ToDto();
            await _eventPublisher.PublishAsync(InvoiceTopics.Cancelled, payload);
            
            return payload;
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

        private async Task CheckClientCreditLimit(Guid clientId, decimal invoiceTotalTTC, Guid? excludeInvoiceId = null)
        {
            var client = await _clientCacheRepository.GetByIdAsync(clientId)
                ?? throw new KeyNotFoundException($"Client with Id: {clientId} not found.");

            var invoices = await _invoiceRepository.GetByClientIdAsNoTrackingAsync(clientId);

            // Exclude current invoice if updating
            var clientCurrentCredit = invoices
                .Where(i => i.Status == InvoiceStatus.UNPAID
                            && (excludeInvoiceId == null || i.Id != excludeInvoiceId))
                .Sum(i => i.TotalTTC);

            if (client.CreditLimit < invoiceTotalTTC + clientCurrentCredit)
                throw new InvoiceDomainException(
                    $"Cannot create invoice. Client '{client.Name}' exceeds credit limit." +
                    $" Current used: {clientCurrentCredit:C}, Attempted invoice: {invoiceTotalTTC:C}, Limit: {client.CreditLimit:C}"
                );
        }

        private void ValidateStockAvailability(List<CreateInvoiceItemDto> items, StockStatusResponse stockStatus)
        {
            var errors = new List<string>();

            // Create lookup dictionary for O(1) access
            var inStockLookup = stockStatus.IN_STOCK.ToDictionary(
                s => s.ArticleId,
                s => s.Quantity
            );

            var outStockLookup = stockStatus.OUT_STOCK.ToDictionary(
                s => s.ArticleId,
                s => s.Quantity
            );

            foreach (var item in items)
            {
                decimal availableQuantity = 0;

                // Check if article has positive stock
                if (inStockLookup.TryGetValue(item.ArticleId, out var inStock))
                {
                    availableQuantity = inStock;
                }
                // Check if article has negative stock (oversold)
                else if (outStockLookup.TryGetValue(item.ArticleId, out var outStock))
                {
                    // Article has negative stock - cannot fulfill
                    errors.Add($"Article {item.ArticleId} has negative stock ({-outStock} units). Cannot fulfill order.");
                    continue;
                }
                // Article not found in any stock (zero stock)
                else
                {
                    availableQuantity = 0;
                }

                // Validate quantity
                if (availableQuantity < item.Quantity)
                {
                    errors.Add($"Article {item.ArticleId} has insufficient stock. " +
                              $"Requested: {item.Quantity}, Available: {availableQuantity}");
                }
            }

            if (errors.Any())
            {
                throw new InvoiceDomainException($"Stock validation failed: {string.Join("; ", errors)}");
            }
        }

    }
}