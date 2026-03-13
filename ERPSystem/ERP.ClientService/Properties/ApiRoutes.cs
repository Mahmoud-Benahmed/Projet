namespace ERP.ClientService.Properties
{
    public static class ApiRoutes
    {
        public static class Clients
        {
            public const string Controller = "clients";

            public const string GetAll = Controller;
            public const string GetById = Controller + "/{id:guid}";
            public const string GetPagedByType = Controller + "/paged/by-type";
            public const string GetPagedDeleted = Controller + "/deleted";
            public const string Stats = Controller + "/stats";
            public const string Create = Controller;
            public const string Update = Controller + "/{id:guid}";
            public const string Delete = Controller + "/delete/{id:guid}";
            public const string Restore = Controller + "/restore/{id:guid}";
        }
    }
}