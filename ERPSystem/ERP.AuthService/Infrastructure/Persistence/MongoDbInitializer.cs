using ERP.AuthService.Domain;
using ERP.AuthService.Domain.Logger;
using MongoDB.Driver;

namespace ERP.AuthService.Infrastructure.Persistence
{
    public static class MongoDbInitializer
    {
        public static async Task InitializeAsync(MongoDbContext context)
        {
            await context.DropDatabaseAsync();

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

            // AuditLogs — query by user, action, and timestamp
            await context.AuditLogs.Indexes.CreateManyAsync([
                // for GetByUserAsync — filters on PerformedBy or TargetUserId

                // lets MongoDB satisfy both the filter and the sort without a separate in-memory sort step.
                new CreateIndexModel<AuditLog>(
                    Builders<AuditLog>.IndexKeys
                        .Ascending(x => x.PerformedBy)
                        .Descending(x => x.Timestamp)),

                new CreateIndexModel<AuditLog>(
                    Builders<AuditLog>.IndexKeys.Ascending(x => x.TargetUserId)),

                // for GetByActionAsync
                new CreateIndexModel<AuditLog>(
                    Builders<AuditLog>.IndexKeys.Ascending(x => x.Action)),
            ]);
        }
    }
}
