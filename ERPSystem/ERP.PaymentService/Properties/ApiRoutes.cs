namespace ERP.PaymentService.Properties;

public static class ApiRoutes
{
    private const string Base = "";

    public static class Payments
    {
        private const string Root = $"{Base}/payments";

        // ──── GET ROUTES ────
        public const string GetAll = Root;
        public const string GetById = $"{Root}/{{id:guid}}";
        public const string GetByInvoice = $"{Root}/invoice/{{invoiceId:guid}}";
        public const string GetByClient = $"{Root}/client/{{clientId:guid}}";
        public const string GetByStatus = $"{Root}/status/{{status}}";
        public const string GetStats = $"{Root}/stats";

        // ──── POST ROUTES ────
        public const string Create = Root;

        // ──── PUT ROUTES ────
        public const string Update = $"{Root}/update/{{id:guid}}";
        public const string Restore = $"{Root}/{{id:guid}}/restore";

        // ──── DELETE ROUTES ────
        public const string Delete = $"{Root}/{{id:guid}}";
    }

    public static class Invoices
    {
        private const string Root = $"{Base}/invoices";

        // ──── GET ROUTES ────
        public const string GetPaymentSummary = $"{Root}/{{id:guid}}/payment-summary";
        public const string GetPaymentsByInvoice = $"{Root}/{{invoiceId:guid}}/payments";
    }

    public static class LateFeePolicies
    {
        private const string Root = $"{Base}/late-fee-policies";

        // ──── GET ROUTES ────
        public const string GetAll = Root;
        public const string GetActive = $"{Root}/active";
        public const string GetById = $"{Root}/{{id:guid}}";

        // ──── POST ROUTES ────
        public const string Create = Root;

        // ──── PUT ROUTES ────
        public const string Update = $"{Root}/update/{{id:guid}}";
        public const string Activate = $"{Root}/{{id:guid}}/activate";

        // ──── DELETE ROUTES ────
        public const string Delete = $"{Root}/{{id:guid}}";
    }
}