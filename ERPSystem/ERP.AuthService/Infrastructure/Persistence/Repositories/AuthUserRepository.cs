using ERP.AuthService.Application.DTOs.AuthUser;
using ERP.AuthService.Application.Interfaces.Repositories;
using ERP.AuthService.Domain;
using Microsoft.IdentityModel.Logging;
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
            => await _collection.Find(x => x.Login == login && x.IsActive).FirstOrDefaultAsync();

        public async Task<AuthUser?> GetByEmailAsync(string email)
            => await _collection.Find(x => x.Email == email && x.IsActive).FirstOrDefaultAsync();

        public async Task<AuthUser?> GetByIdAsync(Guid id)
            => await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();

        public async Task<(List<AuthUser>, int)> GetAllAsync(int pageNumber, int pageSize)
        {
            var totalCount = (int)await _collection.CountDocumentsAsync(_ => true);

            var items = await _collection
                .Find(_ => true)
                .Skip((pageNumber - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<(List<AuthUser>, int)> GetPagedByStatusAsync(bool isActive, int pageNumber, int pageSize)
        {
            var filter = Builders<AuthUser>.Filter.Where(x => x.IsActive == isActive);

            var totalCount =(int) await _collection.CountAsync(filter);

            var items = await _collection
                .Find(filter)
                .Skip((pageNumber - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<(List<AuthUser>, int)> GetPagedByRoleAsync(Guid role, int pageNumber, int pageSize)
        {
            var filter = Builders<AuthUser>.Filter.Where(x => x.RoleId == role && x.IsActive);

            var totalCount = (int) await _collection.CountDocumentsAsync(filter);

            var items = await _collection
                .Find(filter)
                .Skip((pageNumber - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }
        public async Task<bool> ExistsByEmailAsync(string email)
            => await _collection.Find(x => x.Email == email && x.IsActive).AnyAsync();

        public async Task<bool> ExistsByLoginAsync(string login)
            => await _collection.Find(x => x.Login == login && x.IsActive).AnyAsync();

        public async Task<AuthUser?> UpdateAsync(AuthUser user)
        {
            var result = await _collection.ReplaceOneAsync(x => x.Id == user.Id, user);
            return result.ModifiedCount > 0 ? user : null;
        }
        public async Task<long> CountAsync()
            => await _collection.CountDocumentsAsync(x=> x.IsActive);


        public async Task<long> CountByStatusAsync(bool status) =>
            await _collection.CountDocumentsAsync(x=> x.IsActive == status);

        public async Task DeleteAllAsync()
        {
            await _collection.DeleteManyAsync(FilterDefinition<AuthUser>.Empty);
        }

        public async Task<UserStatsDto> GetStatsAsync()
        {
            var total=          (int) await _collection.CountDocumentsAsync(_ => true);
            var active=         (int) await _collection.CountDocumentsAsync(u=> u.IsActive);
            var deactivated=    (int) await _collection.CountDocumentsAsync(u=> !u.IsActive);

            return new UserStatsDto
            {
                TotalUsers = total,
                ActiveUsers = active,
                DeactivatedUsers = deactivated
            };
        }
    }
}
