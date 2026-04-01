namespace ERP.StockService.Properties;

public static class ApiRoutes
{
    private const string Base = "stock";
    public static class BonEntres
    {
        private const string Root = $"{Base}/bon-entres";

        public const string GetAll = Root;
        public const string GetById = $"{Root}/{{id:guid}}";
        public const string GetDeleted = $"{Root}/deleted";
        public const string GetByFournisseur = $"{Root}/by-fournisseur/{{fournisseurId:guid}}";
        public const string GetByDateRange = $"{Root}/by-date-range";
        public const string GetStats = $"{Root}/stats";

        public const string Create = Root;
        public const string Update = $"{Root}/{{id:guid}}";
        public const string Delete = $"{Root}/{{id:guid}}";
    }

    public static class BonSorties
    {
        private const string Root = $"{Base}/bon-sorties";

        public const string GetAll = Root;
        public const string GetById = $"{Root}/{{id:guid}}";
        public const string GetDeleted = $"{Root}/deleted";
        public const string GetByClient = $"{Root}/by-client/{{clientId:guid}}";
        public const string GetByDateRange = $"{Root}/by-date-range";
        public const string GetStats = $"{Root}/stats";

        public const string Create = Root;
        public const string Update = $"{Root}/{{id:guid}}";
        public const string Delete = $"{Root}/{{id:guid}}";
    }
    
    public static class BonRetours
    {
        private const string Root = $"{Base}/bon-retours";

        public const string GetAll = Root;
        public const string GetById = $"{Root}/{{id:guid}}";
        public const string GetDeleted = $"{Root}/deleted";
        public const string GetBySource = $"{Root}/by-source/{{sourceId:guid}}";
        public const string GetByDateRange = $"{Root}/by-date-range";
        public const string GetStats = $"{Root}/stats";

        public const string Create = Root;
        public const string Update = $"{Root}/{{id:guid}}";
        public const string Delete = $"{Root}/{{id:guid}}";
    }

    public static class Fournisseurs
    {
        private const string Root = $"{Base}/fournisseurs";

        public const string GetAll = Root;
        public const string GetById = $"{Root}/{{id:guid}}";
        public const string GetDeleted = $"{Root}/deleted";
        public const string GetByName = $"{Root}/by-name";
        public const string GetStats = $"{Root}/stats";

        public const string Create = Root;
        public const string Update = $"{Root}/{{id:guid}}";
        public const string Delete = $"{Root}/{{id:guid}}";
        public const string Restore = $"{Root}/{{id:guid}}/restore";

        public const string Block = $"{Root}/{{id:guid}}/block";
        public const string Unblock = $"{Root}/{{id:guid}}/unblock";
    }
}