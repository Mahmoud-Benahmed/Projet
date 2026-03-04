namespace ERP.ArticleService.API
{
    public static class ApiRoutes
    {
        private const string Base = "api";

        public static class Categories
        {
            private const string Controller = Base + "/categories";

            public const string GetAll = Controller;
            public const string GetById = Controller + "/{id:guid}";
            public const string GetByName = Controller + "/by-name/{name}";
            public const string GetPaged = Controller + "/paged";
            public const string GetPagedByName = Controller + "/paged/by-name";
            public const string GetPagedByDateRange = Controller + "/paged/by-date-range";
            public const string Create = Controller;
            public const string UpdateName = Controller + "/{id:guid}/name";
            public const string Delete = Controller + "/{id:guid}";
        }

        public static class Articles
        {
            private const string Controller = Base + "/articles";

            public const string GetAll = Controller;
            public const string GetById = Controller + "/{id:guid}";
            public const string GetByCode = Controller + "/by-code/{code}";
            public const string GetPagedByCategory = Controller + "/paged/by-category";
            public const string GetPagedByStatus = Controller + "/paged/by-status";
            public const string GetPagedByLibelle = Controller + "/paged/by-libelle";
            public const string Create = Controller;
            public const string Update = Controller + "/{id:guid}";
            public const string Activate = Controller + "/{id:guid}/activate";
            public const string Deactivate = Controller + "/{id:guid}/deactivate";
            public const string Delete = Controller + "/{id:guid}";
        }

        public static class ArticleCodes
        {
            private const string Controller = Base + "/article-codes";

            public const string Generate = Controller + "/generate";
        }
    }
}