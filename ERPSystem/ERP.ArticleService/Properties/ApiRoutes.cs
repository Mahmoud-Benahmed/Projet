using System.Runtime.CompilerServices;

namespace ERP.ArticleService.API
{
    public static class ApiRoutes
    {

        public static class Categories
        {
            private const string Controller = Articles.Controller + "/categories";

            public const string GetAll = Controller;
            public const string GetById = Controller + "/{id:guid}";
            public const string GetByName = Controller + "/by-name/{name}";
            public const string GetPaged = Controller + "/paged";
            public const string GetPagedByName = Controller + "/paged/by-name";
            public const string GetPagedByDateRange = Controller + "/paged/by-date-range";
            public const string Create = Controller;
            public const string Update = Controller + "/{id:guid}";
            public const string Delete = Controller + "/{id:guid}";
            public const string GetBelowTVA = Controller + "/tva/below";
            public const string GetHigherThanTVA = Controller + "/tva/higher";
            public const string GetBetweenTVA = Controller + "/tva/between";
        }

        public static class Articles
        {
            public const string Controller = "articles";

            public const string GetAll = Controller;
            public const string GetById = Controller + "/{id:guid}";
            public const string GetByCode = Controller + "/by-code/{code}";
            public const string GetPagedByCategory = Controller + "/paged/by-category";
            public const string GetDeletedRoute = Controller + "/deleted";
            public const string GetPagedByLibelle = Controller + "/paged/by-libelle";
            public const string Create = Controller;
            public const string Update = Controller + "/{id:guid}";
            public const string Restore = Controller + "/restore/{id:guid}";
            public const string Delete = Controller + "/delete/{id:guid}";
            public const string Stats = Controller + "/stats";

        }
    }
}