using System;

namespace InvoiceService.Domain
{
    public class InvoiceDomainException : Exception
    {
        public InvoiceDomainException(string message)
            : base(message) { }
    }
}