using ERP.AuthService.Domain;
using ERP.AuthService.Domain.Logger;
using MongoDB.Driver;

namespace ERP.AuthService.Infrastructure.Persistence
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;
        private readonly MongoClient _client;

        public MongoDbContext(string connectionString, string dbName)
        {
            _client = new MongoClient(connectionString);
            _database = _client.GetDatabase(dbName);
        }

        public IMongoCollection<AuditLog> AuditLogs =>
            _database.GetCollection<AuditLog>("AuditLogs");

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

        // MongoDbContext.cs
        public async Task DropCollectionAsync(string collectionName)
            => await _database.DropCollectionAsync(collectionName);

        public async Task DropDatabaseAsync() => await _client.DropDatabaseAsync(_database.DatabaseNamespace.DatabaseName);
    }
}
