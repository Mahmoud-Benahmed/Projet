namespace ERP.StockService.API.Routes;

public static class ApiRoutes
{
    public static class BonEntres
    {
        private const string Root = "/bon-entres";

        public const string GetAll = Root;
        public const string GetById = $"{Root}/{{id:guid}}";
        public const string GetDeleted = $"{Root}/deleted";
        public const string GetByFournisseur = $"{Root}/by-fournisseur/{{fournisseurId:guid}}";
        public const string GetByDateRange = $"{Root}/by-date-range";

        public const string Create = Root;
        public const string Update = $"{Root}/{{id:guid}}";
        public const string Delete = $"{Root}/{{id:guid}}";

        public const string AddLigne = $"{Root}/{{id:guid}}/lignes";
        public const string UpdateLigne = $"{Root}/{{id:guid}}/lignes/{{ligneId:guid}}";
        public const string RemoveLigne = $"{Root}/{{id:guid}}/lignes/{{ligneId:guid}}";
    }

    public static class BonSorties
    {
        private const string Root = "/bon-sorties";

        public const string GetAll = Root;
        public const string GetById = $"{Root}/{{id:guid}}";
        public const string GetDeleted = $"{Root}/deleted";
        public const string GetByClient = $"{Root}/by-client/{{clientId:guid}}";
        public const string GetByDateRange = $"{Root}/by-date-range";

        public const string Create = Root;
        public const string Update = $"{Root}/{{id:guid}}";
        public const string Delete = $"{Root}/{{id:guid}}";

        public const string AddLigne = $"{Root}/{{id:guid}}/lignes";
        public const string UpdateLigne = $"{Root}/{{id:guid}}/lignes/{{ligneId:guid}}";
        public const string RemoveLigne = $"{Root}/{{id:guid}}/lignes/{{ligneId:guid}}";
    }
    
    public static class BonRetours
    {
        private const string Root = "/bon-retours";

        public const string GetAll = Root;
        public const string GetById = $"{Root}/{{id:guid}}";
        public const string GetDeleted = $"{Root}/deleted";
        public const string GetBySource = $"{Root}/by-source/{{sourceId:guid}}";
        public const string GetByDateRange = $"{Root}/by-date-range";

        public const string Create = Root;
        public const string Update = $"{Root}/{{id:guid}}";
        public const string Delete = $"{Root}/{{id:guid}}";

        public const string AddLigne = $"{Root}/{{id:guid}}/lignes";
        public const string UpdateLigne = $"{Root}/{{id:guid}}/lignes/{{ligneId:guid}}";
        public const string RemoveLigne = $"{Root}/{{id:guid}}/lignes/{{ligneId:guid}}";
    }
}