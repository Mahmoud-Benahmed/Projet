using ERP.AuthService.Application.Interfaces;
using ERP.AuthService.Domain;
using ERP.AuthService.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ERP.AuthService.Infrastructure.Persistence
{
    public class MongoAuthUserRepository : IAuthUserRepository
    {
        private readonly IMongoCollection<AuthUser> _collection;

        public MongoAuthUserRepository(
            IMongoClient client,
            IOptions<MongoSettings> settings)
        {
            var database = client.GetDatabase(settings.Value.DatabaseName);
            _collection = database.GetCollection<AuthUser>("AuthUsers");
        }

        public async Task AddAsync(AuthUser user)
            => await _collection.InsertOneAsync(user);

        public async Task<AuthUser?> GetByEmailAsync(string email)
            => await _collection.Find(x => x.Email == email).FirstOrDefaultAsync();

        public async Task<AuthUser?> GetByIdAsync(Guid id)
            => await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();

        public async Task<bool> ExistsByEmailAsync(string email)
            => await _collection.Find(x => x.Email == email).AnyAsync();

        public async Task UpdateAsync(AuthUser user)
            => await _collection.ReplaceOneAsync(x => x.Id == user.Id, user);
    }
}
