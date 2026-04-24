namespace ERP.InvoiceService.Properties;

public static class ApiRoutes
{
    private const string Base = "";
    public static class Invoices
    {
        private const string Root = $"{Base}/invoices";

        public const string ToPdf = $"{Root}/{{id:guid}}/pdf";
        public const string GetAll = Root;
        public const string GetById = $"{Root}/{{id:guid}}";
        public const string GetByClient = $"{Root}/client/{{clientId:guid}}";
        public const string GetByStatus = $"{Root}/status/{{status}}";
        public const string GetStats = $"{Root}/stats";
        // ──── POST ROUTES ────
        public const string Create = Root;
        public const string AddItem = $"{Root}/{{id:guid}}/items";
        // ──── PUT ROUTE ──── 
        public const string Update = $"{Root}/update/{{id:guid}}";
        // ──── DELETE ROUTES ────
        public const string RemoveItem = $"{Root}/{{id:guid}}/items/{{itemId:guid}}";
        public const string Finalize = $"{Root}/{{id:guid}}/finalize";
        public const string MarkAsPaid = $"{Root}/{{id:guid}}/pay";

        public const string Cancel = $"{Root}/{{id:guid}}/cancel";

        // ──── SOFT DELETE ROUTES ────

        public const string Delete = $"{Root}/{{id:guid}}";

        public const string Restore = $"{Root}/{{id:guid}}/restore";

        public static class Cache
        {
            public static class Articles
            {
                private const string Root = $"{Invoices.Root}/cache/articles";
                public const string GetById = $"{Root}/{{id:guid}}";
                public const string GetByBarCode = $"{Root}/by-barcode/{{barcode}}";
                public const string GetByRefCode = $"{Root}/by-refcode/{{refcode}}";
                public const string GetPaged = Root;
            }

            public static class Clients
            {
                private const string Root = $"{Invoices.Root}/cache/clients";
                public const string GetById = $"{Root}/{{id:guid}}";
                public const string GetPaged = Root;

            }
        }
    }
}