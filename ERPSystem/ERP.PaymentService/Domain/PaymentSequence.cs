namespace ERP.PaymentService.Domain;

public class PaymentSequence
{
    public Guid Id { get; private set; }
    public int Year { get; private set; }
    public int CurrentNumber { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private PaymentSequence() { }

    public PaymentSequence(int year)
    {
        Id = Guid.NewGuid();
        Year = year;
        CurrentNumber = 0;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public int GetNextNumber()
    {
        CurrentNumber++;
        UpdatedAt = DateTime.UtcNow;
        return CurrentNumber;
    }

    public string FormatPaymentNumber()
    {
        return $"PAY-{Year}-{CurrentNumber:D5}";
    }
}