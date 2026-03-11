using ERP.AuthService.Application.DTOs.AuthUser;
using ERP.AuthService.Application.Interfaces.Repositories;
using ERP.AuthService.Domain;
using Microsoft.IdentityModel.Logging;
using MongoDB.Driver;
using static System.Net.WebRequestMethods;

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

        public async Task<(List<AuthUser>, int)> GetAllAsync(int pageNumber, int pageSize, Guid? excludeId = null)
        {
            return await GetPagedAsync(pageNumber, pageSize, excludeId);
        }

        public async Task<(List<AuthUser>, int)> GetPagedByStatusAsync(bool status, int pageNumber, int pageSize, Guid? excludeId = null)
        {
            var filter = Builders<AuthUser>.Filter.Where(u => u.IsActive == status);

            return await GetPagedAsync(pageNumber, pageSize, excludeId, filter);
        }

        public async Task<(List<AuthUser>, int)> GetPagedByRoleAsync(Guid role, int pageNumber, int pageSize, Guid? excludeId = null)
        {
            var filter = Builders<AuthUser>.Filter.Where(u => u.RoleId == role && u.IsActive);

            return await GetPagedAsync(pageNumber, pageSize, excludeId, filter);
        }

        public async Task<(List<AuthUser>, int)> GetDeletedPagedAsync(int pageNumber, int pageSize,Guid? excludeId = null)
        {
            return await GetPagedAsync(pageNumber, pageSize, excludeId, includeDeleted: true);
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

        public async Task<long> DeleteAsync(Guid userId)
        {
            var result= await _collection.DeleteOneAsync(u => u.Id == userId);
            return result.DeletedCount;
        }
        public async Task<long> DeleteAllAsync()
        {
            var result =await _collection.DeleteManyAsync(FilterDefinition<AuthUser>.Empty);
            return result.DeletedCount;
        }

        public async Task<UserStatsDto> GetStatsAsync(Guid? excludeId = default)
        {
            var baseFilter = excludeId.HasValue
                                ? Builders<AuthUser>.Filter.Where(x => x.Id != excludeId.Value)
                                : Builders<AuthUser>.Filter.Empty;

            var notDeletedFilter = Builders<AuthUser>.Filter.And(baseFilter,
                Builders<AuthUser>.Filter.Where(x => !x.IsDeleted));

            var deletedFilter = Builders<AuthUser>.Filter.And(baseFilter,
                Builders<AuthUser>.Filter.Where(x => x.IsDeleted));

            var activeFilter = Builders<AuthUser>.Filter.And(notDeletedFilter,
                Builders<AuthUser>.Filter.Where(x => x.IsActive));

            var inactiveFilter = Builders<AuthUser>.Filter.And(notDeletedFilter,
                Builders<AuthUser>.Filter.Where(x => !x.IsActive));

            var totalUsers = (int)await _collection.CountDocumentsAsync(notDeletedFilter);
            var activeUsers = (int)await _collection.CountDocumentsAsync(activeFilter);
            var deactivatedUsers = (int)await _collection.CountDocumentsAsync(inactiveFilter);
            var deletedUsers = (int)await _collection.CountDocumentsAsync(deletedFilter);

            return new UserStatsDto
            {
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                DeactivatedUsers = deactivatedUsers,
                DeletedUsers = deletedUsers
            };
        }


        private async Task<(List<AuthUser>, int)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            Guid? excludeId = null,
            FilterDefinition<AuthUser>? filter = null,
            SortDefinition<AuthUser>? sort = null,
            bool includeDeleted = false
        )
        {
            pageNumber = Math.Max(pageNumber, 1);
            pageSize = Math.Max(pageSize, 1);

            var filters = new List<FilterDefinition<AuthUser>>();

            // deleted filter
            if (!includeDeleted)
                filters.Add(Builders<AuthUser>.Filter.Where(u => !u.IsDeleted));
            else
                filters.Add(Builders<AuthUser>.Filter.Where(u => u.IsDeleted));

            // custom filter
            if (filter != null)
                filters.Add(filter);

            // excludeId filter
            if (excludeId.HasValue)
                filters.Add(Builders<AuthUser>.Filter.Where(u => u.Id != excludeId.Value));

            var finalFilter = Builders<AuthUser>.Filter.And(filters);

            sort ??= Builders<AuthUser>.Sort.Ascending(u => u.CreatedAt);

            var totalCount = (int)await _collection.CountDocumentsAsync(finalFilter);

            var items = await _collection
                .Find(finalFilter)
                .Sort(sort)
                .Skip((pageNumber - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }
    }
}
