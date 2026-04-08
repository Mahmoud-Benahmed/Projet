namespace ERP.FournisseurService.Properties;

public static class ApiRoutes
{
    public const string Base = "";
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
