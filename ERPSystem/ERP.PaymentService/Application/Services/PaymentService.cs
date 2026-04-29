using ERP.PaymentService.Application.DTO;
using ERP.PaymentService.Application.Exceptions;
using ERP.PaymentService.Application.Interfaces;
using ERP.PaymentService.Application.Interfaces.LocalCache;
using ERP.PaymentService.Domain;
using ERP.PaymentService.Infrastructure.Messaging;
using ERP.PaymentService.Infrastructure.Persistence.Repositories;

namespace ERP.PaymentService.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IInvoiceCacheRepository _invoiceCacheRepository;
    private readonly IPaymentNumberGenerator _numberGenerator;
    private readonly ILogger<PaymentService> _logger;
    private readonly IEventPublisher _eventPublisher;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IInvoiceCacheRepository invoiceCacheRepository,
        IPaymentNumberGenerator numberGenerator,
        ILogger<PaymentService> logger,
        IEventPublisher eventPublisher)
    {
        _numberGenerator = numberGenerator;
        _paymentRepository = paymentRepository;
        _invoiceCacheRepository = invoiceCacheRepository;
        _logger = logger;
        _eventPublisher = eventPublisher;
    }

    public async Task<PaymentDto> GetByIdAsync(Guid id)
    {
        var payment = await _paymentRepository.GetByIdAsync(id) ?? throw new PaymentNotFoundException(id);
        return ToDto(payment);
    }

    public async Task<PaymentDto> GetByNumberAsync(string number)
    {
        var payment = await _paymentRepository.GetByNumberAsync(number) ?? throw new PaymentNotFoundException(number);
        return ToDto(payment);
    }

    public async Task<PagedResultDto<PaymentDto>> GetByClientIdAsync(Guid clientId, int pageNumber, int pageSize)
    {
        var items = await _paymentRepository.GetByClientIdAsync(clientId);

        return new PagedResultDto<PaymentDto>(
            items.Select(ToDto).ToList(),
            items.Count,
            pageNumber,
            pageSize);
    }

    public async Task<PagedResultDto<PaymentDto>> GetPagedAsync(int pageNumber, int pageSize, string? search = null){
        var (items, totalCount) = await _paymentRepository.GetPagedAsync(
            pageNumber, pageSize, search);

        return new PagedResultDto<PaymentDto>(
            items.Select(ToDto).ToList(),
            totalCount,
            pageNumber,
            pageSize);
    }

    public async Task<List<PaymentSummaryDto>> GetSummaryByInvoiceIdAsync(Guid invoiceId)
    {
        // verify invoice exists in cache before querying payments
        var invoiceExists = await _invoiceCacheRepository.GetByIdAsync(invoiceId);
        if (invoiceExists is null)
        {
            _logger.LogWarning(
                "Invoice {InvoiceId} not found in cache.", invoiceId);
            return [];
        }

        return await _paymentRepository.GetSummaryByInvoiceIdAsync(invoiceId);
    }

    public async Task<PaymentDto> CreateAsync(CreatePaymentDto dto)
    {
        var allocationsSum = dto.Allocations.Sum(a => a.AmountAllocated);
        if (Math.Abs(allocationsSum - dto.TotalAmount) > 0.01m)
            throw new PaymentDomainException(
                $"TotalAmount ({dto.TotalAmount:F2}) must equal the sum of all allocations ({allocationsSum:F2}).");

        // 1. validate all invoice ids exist in cache
        var invoiceIds = dto.Allocations.Select(a => a.InvoiceId).Distinct().ToList();

        var cacheEntries = new List<Domain.LocalCache.InvoiceCache>();
        foreach (var invoiceId in invoiceIds)
        {
            var cache = await _invoiceCacheRepository.GetByIdAsync(invoiceId);
            if (cache is null)
                throw new InvoiceNotFoundException(invoiceId);

            if (cache.Status == Domain.LocalCache.InvoiceStatus.CANCELLED)
                throw new InvoiceAlreadyCancelledException(invoiceId);

            if (cache.Status == Domain.LocalCache.InvoiceStatus.PAID)
                throw new InvoiceAlreadyPaidException(invoiceId);

            cacheEntries.Add(cache);
        }

        string paymentNumber = await _numberGenerator.GenerateNextPaymentNumberAsync();

        // 2. build the payment aggregate
        var payment = new Payment(
            number: paymentNumber,
            clientId: dto.ClientId,
            totalAmount: dto.TotalAmount,
            method: dto.Method,
            paymentDate: dto.PaymentDate,
            externalReference: dto.ExternalReference,
            notes: dto.Notes
        );

        // 3. allocate each invoice — domain validates amounts
        foreach (var allocation in dto.Allocations)
        {
            var cache = cacheEntries.First(c => c.Id == allocation.InvoiceId);
            payment.AllocateAmount(allocation.AmountAllocated, cache);
        }

        // 4. persist
        await _paymentRepository.AddAsync(payment);

        _logger.LogInformation(
            "Payment {Number} created. Id: {PaymentId}, " +
            "TotalAmount: {TotalAmount}, Allocations: {Count}",
            payment.Number, payment.Id,
            payment.TotalAmount, payment.Allocations.Count);

        // 5. update cache and check for fully paid invoices
        foreach (var allocation in dto.Allocations)
        {
            var cache = cacheEntries.First(c => c.Id == allocation.InvoiceId);
            cache.ApplyPayment(allocation.AmountAllocated);
            await _invoiceCacheRepository.SaveChangesAsync(cache);

            // publish event if invoice is now fully paid
            if (cache.Status == Domain.LocalCache.InvoiceStatus.PAID)
            {
                await _eventPublisher.PublishAsync(
                    PaymentTopics.InvoicePaid,
                    new InvoicePaidEvent(
                        InvoiceId: cache.Id,
                        PaymentId: payment.Id,
                        PaidAmount: cache.PaidAmount,
                        PaidAt: DateTime.UtcNow
                    ));

                _logger.LogInformation(
                    "Invoice {InvoiceId} fully paid via Payment {PaymentId}.",
                    cache.Id, payment.Id);
            }
        }

        _logger.LogInformation(
            "Payment {Number} created. Id: {PaymentId}, " +
            "TotalAmount: {TotalAmount}, Allocations: {Count}",
            payment.Number, payment.Id,
            payment.TotalAmount, payment.Allocations.Count);

        return ToDto(payment);
    }

    public async Task<PaymentDto> CorrectDetailsAsync(Guid id, CorrectPaymentDto dto)
    {
        var payment = await _paymentRepository.GetByIdAsync(id) ?? throw new PaymentNotFoundException(id);

        payment.CorrectDetails(dto.Method, dto.ExternalReference, dto.Notes);

        await _paymentRepository.UpdateAsync(payment);

        return ToDto(payment);
    }

    public async Task CancelAsync(Guid id)
    {
        var payment = await _paymentRepository.GetByIdAsync(id) ?? throw new PaymentNotFoundException(id);

        payment.Cancel();

        await _paymentRepository.UpdateAsync(payment);

        _logger.LogInformation(
            "Payment {PaymentId} cancelled at {CancelledAt}.",
            id, payment.CancelledAt);
    }

    // ── mapping ────────────────────────────────────────────────
    private static PaymentDto ToDto(Payment p) => new(
        Id: p.Id,
        Number: p.Number,
        ClientId: p.ClientId,
        TotalAmount: p.TotalAmount,
        RemainingAmount: p.GetRemainingAmount(),
        Method: p.Method.ToString(),
        PaymentDate: p.PaymentDate,
        ExternalReference: p.ExternalReference,
        Notes: p.Notes,
        IsCancelled: p.CancelledAt is not null,
        CancelledAt: p.CancelledAt,
        Allocations: p.Allocations.Select(a => new PaymentAllocationDto(
                               Id: a.Id,
                               InvoiceId: a.InvoiceId,
                               AmountAllocated: a.AmountAllocated
                           )).ToList()
    );
}