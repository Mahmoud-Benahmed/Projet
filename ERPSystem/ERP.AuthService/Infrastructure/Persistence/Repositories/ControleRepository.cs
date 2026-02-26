using ERP.AuthService.Application.Interfaces.Repositories;
using ERP.AuthService.Domain;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ERP.AuthService.Infrastructure.Persistence.Repositories
{
    public class ControleRepository : IControleRepository
    {
        private readonly IMongoCollection<Controle> _collection;

        public ControleRepository(MongoDbContext context)
        {
            _collection = context.Controles;
        }

        public async Task<Controle?> GetByIdAsync(Guid id)
            => await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();

        public async Task<Controle?> GetByLibelleAsync(string libelle)
            => await _collection.Find(x => x.Libelle == libelle).FirstOrDefaultAsync();

        public async Task<List<Controle>> GetAllAsync()
            => await _collection.Find(_ => true).ToListAsync();

        public async Task<List<Controle>> GetByCategoryAsync(string category)
        {
            var filter = Builders<Controle>.Filter.Regex(
                x => x.Category,
                new BsonRegularExpression(category, "i")); // "i" = case insensitive

            return await _collection.Find(filter).ToListAsync();
        }

        public async Task AddAsync(Controle controle)
            => await _collection.InsertOneAsync(controle);

        public async Task UpdateAsync(Controle controle)
            => await _collection.ReplaceOneAsync(x => x.Id == controle.Id, controle);

        public async Task DeleteAsync(Guid id)
            => await _collection.DeleteOneAsync(x => x.Id == id);

        public async Task<long> CountAsync()
            => await _collection.CountDocumentsAsync(_ => true);
    }
}