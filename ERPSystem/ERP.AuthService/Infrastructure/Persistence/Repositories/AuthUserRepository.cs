using ERP.AuthService.Application.Interfaces.Repositories;
using ERP.AuthService.Domain;
using MongoDB.Driver;

namespace ERP.AuthService.Infrastructure.Persistence.Repositories
{
    public class AuthUserRepository : IAuthUserRepository
    {
        private readonly IMongoCollection<AuthUser> _collection;

        public AuthUserRepository(MongoDbContext context)
        {
            _collection = context.AuthUsers;
        }

        public async Task AddAsync(AuthUser user)
            => await _collection.InsertOneAsync(user);

        public async Task<AuthUser?> GetByLoginAsync(string login)
            => await _collection.Find(x => x.Login == login).FirstOrDefaultAsync();

        public async Task<AuthUser?> GetByEmailAsync(string email)
            => await _collection.Find(x => x.Email == email).FirstOrDefaultAsync();

        public async Task<AuthUser?> GetByIdAsync(Guid id)
            => await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();

        public async Task<bool> ExistsByEmailAsync(string email)
            => await _collection.Find(x => x.Email == email).AnyAsync();

        public async Task<bool> ExistsByLoginAsync(string login)
            => await _collection.Find(x => x.Login == login).AnyAsync();

        public async Task UpdateAsync(AuthUser user)
            => await _collection.ReplaceOneAsync(x => x.Id == user.Id, user);
        public async Task<long> CountAsync()
            => await _collection.CountDocumentsAsync(_ => true);

        public async Task DeleteAllAsync()
        {
            await _collection.DeleteManyAsync(FilterDefinition<AuthUser>.Empty);
        }
    }
}
