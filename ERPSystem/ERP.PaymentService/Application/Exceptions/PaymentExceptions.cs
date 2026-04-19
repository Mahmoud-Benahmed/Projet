namespace ERP.PaymentService.Application.Exceptions
{
    public class PaymentNotFoundException : Exception
    {
        public PaymentNotFoundException(Guid id)
            : base($"Payment with Id '{id}' was not found.") { }
    }

    public class InvoiceNotFoundException : Exception
    {
        public InvoiceNotFoundException(Guid id)
            : base($"Invoice with Id '{id}' was not found.") { }
    }

    public class InvoiceAlreadyPaidException : Exception
    {
        public InvoiceAlreadyPaidException(Guid invoiceId)
            : base($"Invoice '{invoiceId}' has already been fully paid.") { }
    }

    public class InvoiceCancelledException : Exception
    {
        public InvoiceCancelledException(Guid invoiceId)
            : base($"Invoice '{invoiceId}' is cancelled and cannot receive payments.") { }
    }

    public class ClientBlockedException : Exception
    {
        public ClientBlockedException(Guid clientId)
            : base($"Client '{clientId}' is blocked and cannot make payments.") { }
    }

    public class NoActiveLateFeePolicyException : Exception
    {
        public NoActiveLateFeePolicyException()
            : base("No active late fee policy found.") { }
    }

    public class LateFeePolicyNotFoundException : Exception
    {
        public LateFeePolicyNotFoundException(Guid id)
            : base($"Late fee policy with Id '{id}' was not found.") { }
    }
}
