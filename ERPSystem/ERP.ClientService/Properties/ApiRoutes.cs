namespace ERP.ClientService.Properties;

public static class ApiRoutes
{
    private const string Base = "";

    public static class Clients
    {
        public const string Root = $"{Base}/clients";

        public const string GetAll = Root;
        public const string GetById = $"{Root}/{{id:guid}}";
        public const string GetDeleted = $"{Root}/deleted";
        public const string GetByCategory = $"{Root}/by-category";
        public const string GetByName = $"{Root}/by-name";
        public const string Stats = $"{Root}/stats";

        public const string Create = Root;
        public const string Update = $"{Root}/{{id:guid}}";
        public const string Delete = $"{Root}/{{id:guid}}";
        public const string Restore = $"{Root}/restore/{{id:guid}}";

        public const string Block = $"{Root}/block/{{id:guid}}";
        public const string Unblock = $"{Root}/unblock/{{id:guid}}";

        public const string EffectiveDelaiRetour = $"{Root}/{{id:guid}}/return-window/effective";
        public const string CanPlaceOrder = $"{Root}/{{id:guid}}/can-place-order";

        public const string AddCategory = $"{Root}/{{id:guid}}/categories";
        public const string RemoveCategory = $"{Root}/{{id:guid}}/categories/{{categoryId:guid}}";
    }

    public static class Categories
    {
        private const string Root = $"{Clients.Root}/categories";

        public const string GetAll = Root;
        public const string GetAllPaged = $"{Root}/paged";
        public const string GetById = $"{Root}/{{id:guid}}";
        public const string GetDeleted = $"{Root}/deleted";
        public const string GetByName = $"{Root}/by-name";
        public const string Stats = $"{Root}/stats";

        public const string Create = Root;
        public const string Update = $"{Root}/{{id:guid}}";
        public const string Delete = $"{Root}/{{id:guid}}";
        public const string Restore = $"{Root}/restore/{{id:guid}}";
        public const string Activate = $"{Root}/activate/{{id:guid}}";
        public const string Deactivate = $"{Root}/deactivate/{{id:guid}}";
    }
}