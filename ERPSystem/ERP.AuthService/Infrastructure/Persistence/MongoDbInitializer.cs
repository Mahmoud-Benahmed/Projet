using ERP.AuthService.Domain;
using MongoDB.Driver;

namespace ERP.AuthService.Infrastructure.Persistence
{
    public static class MongoDbInitializer
    {
        public static async Task InitializeAsync(MongoDbContext context)
        {
            // AuthUsers — unique email
            await context.AuthUsers.Indexes.CreateOneAsync(
                new CreateIndexModel<AuthUser>(
                    Builders<AuthUser>.IndexKeys.Ascending(u => u.Email),
                    new CreateIndexOptions
                    {
                        Unique = true,
                        Collation = new Collation("en", strength: CollationStrength.Primary)
                    }));

            // Controles — unique libelle
            await context.Controles.Indexes.CreateOneAsync(
                new CreateIndexModel<Controle>(
                    Builders<Controle>.IndexKeys.Ascending(c => c.Libelle),
                    new CreateIndexOptions { Unique = true }));

            // Roles — unique libelle
            await context.Roles.Indexes.CreateOneAsync(
                new CreateIndexModel<Role>(
                    Builders<Role>.IndexKeys.Ascending(r => r.Libelle),
                    new CreateIndexOptions { Unique = true }));

            // Privileges — unique roleId + controleId combination
            await context.Privileges.Indexes.CreateOneAsync(
                new CreateIndexModel<Privilege>(
                    Builders<Privilege>.IndexKeys
                        .Ascending(p => p.RoleId)
                        .Ascending(p => p.ControleId),
                    new CreateIndexOptions { Unique = true }));
        }
    }
}
