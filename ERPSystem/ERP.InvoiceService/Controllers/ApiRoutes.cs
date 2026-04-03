namespace ERP.InvoiceService.Controllers
{
    public static class ApiRoutes
    {
        private const string Base = "api";
        public static class Invoices
        {
            private const string Root = $"{Base}/invoices";
            public const string GetAll = Root;
            public const string GetById = $"{Root}/{{id:guid}}";
            public const string GetByClient = $"{Root}/client/{{clientId:guid}}";
            public const string GetByStatus = $"{Root}/status/{{status}}";
            // ──── POST ROUTES ────
            public const string Create = Root;
            public const string AddItem = $"{Root}/{{id:guid}}/items";
            // ──── DELETE ROUTES ────
            public const string RemoveItem = $"{Root}/{{id:guid}}/items/{{itemId:guid}}";
            public const string Finalize = $"{Root}/{{id:guid}}/finalize";
            public const string MarkAsPaid = $"{Root}/{{id:guid}}/pay";

            public const string Cancel = $"{Root}/{{id:guid}}/cancel";

            // ──── SOFT DELETE ROUTES ────

            public const string Delete = $"{Root}/{{id:guid}}";

            public const string Restore = $"{Root}/{{id:guid}}/restore";
        }
    }
}