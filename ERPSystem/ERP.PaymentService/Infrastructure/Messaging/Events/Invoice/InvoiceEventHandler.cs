using ERP.PaymentService.Application.DTO;
using ERP.PaymentService.Application.Interfaces;
using ERP.PaymentService.Application.Interfaces.LocalCache;

namespace ERP.PaymentService.Infrastructure.Messaging.Events.Invoice;

public sealed class InvoiceEventHandler : IInvoiceEventHandler
{
    private readonly IInvoiceCacheService _invoiceService;
    private readonly IRefundService _refundService;
    private readonly IPaymentInvoiceRepository _paymentInvoiceRepository;
    private readonly ILogger<InvoiceEventHandler> _logger;

    public InvoiceEventHandler(IInvoiceCacheService cacheService,
                                IPaymentInvoiceRepository paymentInvoiceRepository,
                                IRefundService refundService,
                                ILogger<InvoiceEventHandler> logger)
    {
        _paymentInvoiceRepository = paymentInvoiceRepository;
        _refundService = refundService;
        _invoiceService = cacheService;
        _logger = logger;
    }

    public async Task HandleCreatedAsync(InvoiceEventDto dto)
    {
        _logger.LogInformation(
            "Invoice Created | InvoiceId: {InvoiceId} | ClientId: {ClientId}",
            dto.Id,
            dto.ClientId);

        await _invoiceService.SyncCreatedAsync(dto);
    }

    public async Task HandleCancelledAsync(InvoiceEventDto dto)
    {
        _logger.LogInformation(
            "Invoice Cancelled START | InvoiceId: {InvoiceId} | ClientId: {ClientId}",
            dto.Id,
            dto.ClientId);

        await _refundService.CreateRefundAsync(dto.ClientId, dto.Id);

        var allocations = await _paymentInvoiceRepository.GetByInvoiceIdAsync(dto.Id);
        foreach (var alloc in allocations)
        {
            var refundable = alloc.AmountAllocated - alloc.RefundedAmount;

            if (refundable > 0)
            {
                alloc.Refund(refundable);
            }
        }

        await _paymentInvoiceRepository.SaveChangesAsync();

        _logger.LogInformation(
            "Refund Created for Invoice | InvoiceId: {InvoiceId}",
            dto.Id);

        await _invoiceService.SyncCancelledAsync(dto);

        _logger.LogInformation(
            "Invoice Cancelled COMPLETED | InvoiceId: {InvoiceId}",
            dto.Id);
    }
}