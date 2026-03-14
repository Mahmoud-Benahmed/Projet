using ERP.ClientService.Application.DTOs;
using ERP.ClientService.Application.Interfaces;
using ERP.ClientService.Domain;
using ERP.ClientService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERP.ClientService.Infrastructure.Repositories
{
    public class ClientRepository : IClientRepository
    {
        private readonly ClientDbContext _context;

        public ClientRepository(ClientDbContext context)
        {
            _context = context;
        }

        private IQueryable<Client> BaseQuery() =>
            _context.Clients.AsQueryable();

        // =========================
        // CREATE
        // =========================
        public async Task AddAsync(Client client)
        {
            await _context.Clients.AddAsync(client);
        }

        // =========================
        // READ - BY ID
        // =========================
        public async Task<Client?> GetByIdAsync(Guid id)
        {
            return await BaseQuery()
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Client?> GetByIdDeletedAsync(Guid id)
        {
            return await BaseQuery()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        // =========================
        // SAVE CHANGES
        // =========================
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        // =========================
        // PAGING / FILTERING
        // =========================
        public async Task<(List<Client> Items, int TotalCount)> GetAllAsync(int pageNumber, int pageSize)
        {
            var query = BaseQuery();
            return await PaginationHelper.ToPagedResultAsync(
                query, pageNumber, pageSize, q => q.OrderBy(c => c.Name));
        }

        public async Task<(List<Client> Items, int TotalCount)> GetPagedByTypeAsync(ClientType type, int pageNumber, int pageSize)
        {
            var query = BaseQuery().Where(c => c.Type == type);
            return await PaginationHelper.ToPagedResultAsync(
                query, pageNumber, pageSize, q => q.OrderBy(c => c.Name));
        }

        public async Task<(List<Client> Items, int TotalCount)> GetPagedDeletedAsync(int pageNumber, int pageSize)
        {
            var query = BaseQuery()
                .IgnoreQueryFilters()
                .Where(c => c.IsDeleted);
            return await PaginationHelper.ToPagedResultAsync(
                query, pageNumber, pageSize, q => q.OrderBy(c => c.Name));
        }

        // =========================
        // STATS
        // =========================
        public async Task<ClientStatsDto> GetStatsAsync()
        {
            var total = await _context.Clients.IgnoreQueryFilters().CountAsync();
            var active = await _context.Clients.CountAsync();
            var deleted = await _context.Clients.IgnoreQueryFilters().CountAsync(c => c.IsDeleted);

            return new ClientStatsDto(
                TotalCount: total,
                ActiveCount: active,
                DeletedCount: deleted
            );
        }
    }
}