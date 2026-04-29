using ERP.PaymentService.Application.DTO;

namespace ERP.PaymentService.Domain.LocalCache;

public class InvoiceCache
{
    public Guid Id { get; private set; }
    public string InvoiceNumber { get; private set; } = default!;
    public decimal TotalTTC { get; private set; }
    public Guid ClientId { get; private set; }
    public InvoiceStatus Status { get; private set; }
    public decimal PaidAmount { get; private set; }
    public DateTimeOffset LastUpdated { get; private set; }

    private InvoiceCache() { }

    public static InvoiceCache From(InvoiceEventDto e) => new()
    {
        Id = e.Id,
        ClientId = e.ClientId,
        TotalTTC = e.TotalTTC,
        InvoiceNumber = e.InvoiceNumber,
        PaidAmount = 0,
        Status = InvoiceStatus.UNPAID,
        LastUpdated = DateTimeOffset.UtcNow
    };

    // Called when PaymentCreatedEvent is processed
    public void ApplyPayment(decimal amount)
    {
        if(Status == InvoiceStatus.CANCELLED)
            throw new InvalidOperationException("Cannot apply payment to a cancelled invoice.");
        if(Status == InvoiceStatus.PAID)
            throw new InvalidOperationException("Cannot apply payment to an already paid invoice.");
        
        PaidAmount += amount;
        Status = PaidAmount >= TotalTTC
            ? InvoiceStatus.PAID
            : InvoiceStatus.UNPAID;
        LastUpdated = DateTimeOffset.UtcNow;
    }

    // Called when InvoiceCancelledEvent is consumed
    public void MarkCancelled()
    {
        Status = InvoiceStatus.CANCELLED;
        LastUpdated = DateTimeOffset.UtcNow;
    }
}

public enum InvoiceStatus
{
    DRAFT,
    UNPAID,
    PAID,
    CANCELLED
}