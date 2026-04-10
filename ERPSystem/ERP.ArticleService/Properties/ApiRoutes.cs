namespace ERP.ArticleService.Properties;

public static class ApiRoutes
{

    public static class Categories
    {
        private const string Controller = Articles.Base + "/categories";

        public const string GetAll = Controller;
        public const string GetPaged = Controller + "/paged";
        public const string GetDeleted = Controller + "/deleted";
        public const string Create = Controller;
        public const string Update = Controller + $"/{{id:guid}}";
        public const string Delete = Controller + $"/{{id:guid}}";
        public const string Restore = Controller + $"/restore/{{id:guid}}";
        public const string GetById = Controller + $"/{{id:guid}}";
        public const string Stats = Controller + "/stats";
        public const string GetByName = Controller + "/by-name";
        public const string GetByDateRange = Controller + "/by-date-range";
        public const string GetBelowTVA = Controller + "/tva/below";
        public const string GetHigherThanTVA = Controller + "/tva/higher";
        public const string GetBetweenTVA = Controller + "/tva/between";
    }

    public static class Articles
    {
        public const string Base = "articles";

        public const string GetAll = Base;
        public const string GetById = Base + $"/{{id:guid}}";
        public const string ExistsById = Base + $"/{{id:guid}}/exists";
        public const string GetByCode = Base + "/by-code";
        public const string GetPagedByCategory = Base + "/by-category";
        public const string GetDeletedRoute = Base + "/deleted";
        public const string GetPagedByLibelle = Base + "/by-libelle";
        public const string Create = Base;
        public const string Update = Base + $"/{{id:guid}}";
        public const string Restore = Base + "/restore/{id:guid}";
        public const string Delete = Base + $"/{{id:guid}}";
        public const string Stats = Base + "/stats";

    }
}