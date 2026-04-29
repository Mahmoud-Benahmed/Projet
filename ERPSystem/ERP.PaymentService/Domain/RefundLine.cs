namespace ERP.PaymentService.Domain;

public class RefundLine
{
    public Guid PaymentId { get; private set; }
    public Guid PaymentAllocationId { get; private set; }
    public decimal Amount { get; private set; }

    private RefundLine() { }

    public RefundLine(Guid paymentId, Guid allocationId, decimal amount)
    {
        PaymentId = paymentId;
        PaymentAllocationId = allocationId;
        Amount = amount;
    }
}
