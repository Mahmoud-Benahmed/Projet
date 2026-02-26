namespace ERP.AuthService.Properties
{
    public static class ApiRoutes
    {
        private const string Base = "auth";

        public static class Auth
        {
            public const string Base = $"{ApiRoutes.Base}";
        }

        public static class Roles
        {
            public const string Base = $"{ApiRoutes.Base}/roles";
        }

        public static class Controles
        {
            public const string Base = $"{ApiRoutes.Base}/controles";
        }

        public static class Privileges
        {
            public const string Base = $"{ApiRoutes.Base}/privileges";
        }
    }
}