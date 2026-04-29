namespace ERP.PaymentService.Properties;

public class ApiRoutes
{
    private const string Base = "payment";
    public static class Invoices
    {
        private const string Base = $"{ApiRoutes.Base}/cache/invoices";
        public const string GetAll = Base;
        public const string GetById= Base + "/{id:guid}";
        public const string GetByClient = Base + "/client/{clientId:guid}";
        public const string GetByStatus = Base + "/status/{status}";
    }

    public static class Payments
    {
        public const string GetById = $"{Base}/{{id}}";
        public const string GetByNumber = $"{Base}/number/{{number}}";
        public const string GetByClientId = $"{Base}/client/{{clientId}}";
        public const string GetPaged = $"{Base}";
        public const string GetByInvoiceId = $"{Base}/invoice/{{invoiceId}}";
        public const string Create = $"{Base}";
        public const string CorrectDetails = $"{Base}/{{id}}/details";
        public const string Cancel = $"{Base}/{{id}}/cancel";
    }

    public static class Refunds
    {
        private const string Base = $"{ApiRoutes.Base}/refunds";
        public const string GetById = Base + "/{refundId:guid}";
        public const string Complete = Base + "/{refundId:guid}/complete";
    }
}
