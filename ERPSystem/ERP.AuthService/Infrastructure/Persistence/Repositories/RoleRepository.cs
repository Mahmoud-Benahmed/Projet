using ERP.AuthService.Application.Interfaces.Repositories;
using ERP.AuthService.Domain;
using MongoDB.Driver;

namespace ERP.AuthService.Infrastructure.Persistence.Repositories
{
    public class RoleRepository : IRoleRepository
    {
        private readonly IMongoCollection<Role> _collection;

        public RoleRepository(MongoDbContext context)
        {
            _collection = context.Roles;
        }

        public async Task<Role?> GetByIdAsync(Guid id)
            => await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();

        public async Task<Role?> GetByLibelleAsync(RoleEnum libelle)
            => await _collection.Find(x => x.Libelle == libelle).FirstOrDefaultAsync();

        public async Task<List<Role>> GetAllAsync()
            => await _collection.Find(_ => true).ToListAsync();

        public async Task AddAsync(Role role)
            => await _collection.InsertOneAsync(role);

        public async Task UpdateAsync(Role role)
            => await _collection.ReplaceOneAsync(x => x.Id == role.Id, role);

        public async Task DeleteAsync(Guid id)
            => await _collection.DeleteOneAsync(x => x.Id == id);

        public async Task DeleteAllAsync()
        {
            await _collection.DeleteManyAsync(FilterDefinition<Role>.Empty);
        }

        public async Task<long> CountAsync()
            => await _collection.CountDocumentsAsync(_ => true);
    }
}