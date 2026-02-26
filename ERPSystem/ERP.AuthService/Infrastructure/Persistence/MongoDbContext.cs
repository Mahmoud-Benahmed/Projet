using ERP.AuthService.Domain;
using MongoDB.Driver;

namespace ERP.AuthService.Infrastructure.Persistence
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;

        public MongoDbContext(string connectionString, string dbName)
        {
            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(dbName);
        }

        public IMongoCollection<AuthUser> AuthUsers =>
            _database.GetCollection<AuthUser>("AuthUsers");

        public IMongoCollection<RefreshToken> RefreshTokens =>
            _database.GetCollection<RefreshToken>("RefreshTokens");

        public IMongoCollection<Role> Roles =>
            _database.GetCollection<Role>("Roles");

        public IMongoCollection<Controle> Controles =>
            _database.GetCollection<Controle>("Controles");

        public IMongoCollection<Privilege> Privileges =>
            _database.GetCollection<Privilege>("Privileges");
    }
}
