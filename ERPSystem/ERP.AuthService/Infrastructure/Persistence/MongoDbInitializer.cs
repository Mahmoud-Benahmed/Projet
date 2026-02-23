using ERP.AuthService.Domain;
using MongoDB.Driver;

namespace ERP.AuthService.Infrastructure.Persistence
{
    public static class MongoDbInitializer
    {
        public static async Task InitializeAsync(IMongoDatabase database)
        {
            var collection = database.GetCollection<AuthUser>("AuthUsers");

            var indexKeys = Builders<AuthUser>.IndexKeys.Ascending(u => u.Email);
            var indexOptions = new CreateIndexOptions { 
                Unique = true , 
                Collation = new Collation("en", strength: CollationStrength.Primary) 
            };

            await collection.Indexes.CreateOneAsync(new CreateIndexModel<AuthUser>(indexKeys, indexOptions));
        }
    }
}
