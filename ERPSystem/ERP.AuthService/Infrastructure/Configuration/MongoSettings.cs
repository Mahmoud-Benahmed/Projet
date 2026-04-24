namespace ERP.AuthService.Infrastructure.Configuration
{
    public class MongoSettings
    {
        public string ConnectionString { get; set; } = default!;
        public string DatabaseName { get; set; } = default!;
        public string Username { get; set; } = default!;
        public required string Password { get; set; }
    }
}
