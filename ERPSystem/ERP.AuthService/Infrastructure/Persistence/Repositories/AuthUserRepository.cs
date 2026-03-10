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

        public async Task<(List<AuthUser>, int)> GetAllAsync(
            int pageNumber,
            int pageSize,
            Guid? excludeId = default)
        {
            var filter = Builders<AuthUser>.Filter.Empty;

            if (excludeId.HasValue && excludeId != default)
                filter &= Builders<AuthUser>.Filter.Where(x => x.Id != excludeId.Value);

            var totalCount = (int)await _collection.CountDocumentsAsync(filter);
            var items = await _collection
                .Find(filter)
                .Skip((pageNumber - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<(List<AuthUser>, int)> GetPagedByStatusAsync(
            bool isActive,
            int pageNumber,
            int pageSize,
            Guid? excludeId = default)
        {
            var filter = Builders<AuthUser>.Filter.Where(x => x.IsActive == isActive);

            if (excludeId.HasValue && excludeId != default)
                filter &= Builders<AuthUser>.Filter.Where(x => x.Id != excludeId.Value);

            var totalCount = (int)await _collection.CountDocumentsAsync(filter);
            var items = await _collection
                .Find(filter)
                .Skip((pageNumber - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<(List<AuthUser>, int)> GetPagedByRoleAsync(
            Guid role,
            int pageNumber,
            int pageSize,
            Guid? excludeId = default)
        {
                var filter = Builders<AuthUser>.Filter.Where(x => x.RoleId == role && x.IsActive);

                if (excludeId.HasValue && excludeId != default)
                    filter &= Builders<AuthUser>.Filter.Where(x => x.Id != excludeId.Value);

                var totalCount = (int)await _collection.CountDocumentsAsync(filter);
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

        public async Task<UserStatsDto> GetStatsAsync(Guid? excludeId = default)
        {
            var excludeFilter = excludeId.HasValue && excludeId != default
                ? Builders<AuthUser>.Filter.Where(x => x.Id != excludeId.Value)
                : Builders<AuthUser>.Filter.Empty;

            var totalUsers = (int)await _collection.CountDocumentsAsync(excludeFilter);
            var activeFilter = excludeFilter & Builders<AuthUser>.Filter.Where(x => x.IsActive);
            var inactiveFilter = excludeFilter & Builders<AuthUser>.Filter.Where(x => !x.IsActive);
            var activeUsers = (int)await _collection.CountDocumentsAsync(activeFilter);
            var deactivatedUsers = (int)await _collection.CountDocumentsAsync(inactiveFilter);

            return new UserStatsDto
            {
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                DeactivatedUsers = deactivatedUsers
            };
        }
    }
}
