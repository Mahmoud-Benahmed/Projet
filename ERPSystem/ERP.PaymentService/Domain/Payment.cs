using ERP.PaymentService.Application.Exceptions;
using ERP.PaymentService.Domain.LocalCache;

namespace ERP.PaymentService.Domain;
public class Payment
{
    public Guid Id { get; private set; }
    public string Number { get; private set; }
    public Guid ClientId { get; private set; }
    public decimal TotalAmount { get; private set; }
    public PaymentMethod Method { get; private set; }
    public DateTime PaymentDate { get; private set; }
    public string? ExternalReference { get; private set; } = null;
    public string? Notes { get; private set; }
    public DateTime? CancelledAt { get; private set; }



    private readonly List<PaymentInvoice> _allocations = new();
    public IReadOnlyCollection<PaymentInvoice> Allocations => _allocations;

    public decimal GetRemainingAmount() => TotalAmount - _allocations.Sum(a => a.AmountAllocated);


    private Payment() { }
    public Payment(string number, Guid clientId, decimal totalAmount, 
                    PaymentMethod method, DateTime paymentDate, 
                    string? externalReference = null, 
                    string? notes = null)
    {
        Id= Guid.NewGuid();
        Number = number;
        ClientId = clientId;
        TotalAmount = totalAmount;
        Method = method;
        PaymentDate = paymentDate;
        ExternalReference = externalReference;
        Notes = notes;
    }

    public void CorrectDetails(
        PaymentMethod method,
        string? externalReference,
        string? notes)
    {
        if (CancelledAt is not null)
            throw new PaymentAlreadyCancelledException(Id);

        Method = method;
        ExternalReference = externalReference;
        Notes = notes;
    }

    public void AllocateAmount(decimal amount, InvoiceCache cache)
    {
        if (amount <= 0)
            throw new PaymentDomainException("Le montant affecté doit être positif.");

        if (amount> GetRemainingAmount())
            throw new PaymentDomainException($"Le montant affecté ({amount}) dépasse le restant du règlement ({GetRemainingAmount()}).");

        var remaining = cache.TotalTTC - cache.PaidAmount;
        if (amount > remaining)
            throw new PaymentDomainException(
                            $"Le montant affecté ({amount}) dépasse le restant de la facture ({remaining}).");

        _allocations.Add(new PaymentInvoice(Id, cache.Id, amount));
    }

    public void Cancel()
    {
        if (CancelledAt != null)
            throw new PaymentAlreadyCancelledException(Id);
        CancelledAt = DateTime.UtcNow;
    }

}

public enum PaymentMethod
{
    ESPECE,
    CHEQUE,
    VIREMENT,
    CARTE_BANCAIRE,
    MOBILE_PAYMENT,
    AUTRE
}