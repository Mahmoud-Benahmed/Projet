using ERP.AuthService.Application.Interfaces.Repositories;
using ERP.AuthService.Domain;
using MongoDB.Driver;

namespace ERP.AuthService.Infrastructure.Persistence.Repositories
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly IMongoCollection<RefreshToken> _collection;

        public RefreshTokenRepository(MongoDbContext context)
        {
            _collection = context.RefreshTokens;
        }

        public async Task AddAsync(RefreshToken token)
            => await _collection.InsertOneAsync(token);

        public async Task<RefreshToken?> GetByTokenAsync(string token)
            => await _collection.Find(x => x.Token == token).FirstOrDefaultAsync();

        public async Task UpdateAsync(RefreshToken refreshToken)
        {
            var update = Builders<RefreshToken>.Update
                .Set(x => x.IsRevoked, refreshToken.IsRevoked)
                .Set(x => x.RevokedAt, refreshToken.RevokedAt);

            await _collection.UpdateOneAsync(
                x => x.Token == refreshToken.Token,
                update
            );
        }


        public async Task RevokeAllByUserIdAsync(Guid userId)
        {
            var update = Builders<RefreshToken>
                .Update
                .Set(x => x.IsRevoked, true)
                .Set(x => x.RevokedAt, DateTime.UtcNow);

            await _collection.UpdateManyAsync(
                x => x.UserId == userId && !x.IsRevoked,
                update);
        }
    }

}
