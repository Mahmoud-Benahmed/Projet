using ERP.ClientService.Application.DTOs;
using ERP.ClientService.Application.Interfaces;
using ERP.ClientService.Domain;
using ERP.ClientService.Application.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace ERP.ClientService.Application.Services
{
    public class ClientService : IClientService
    {
        private readonly IClientRepository _repository;

        public ClientService(IClientRepository repository)
        {
            _repository = repository;
        }

        public async Task<ClientDto> GetByIdAsync(Guid id)
        {
            var client = await _repository.GetByIdAsync(id) ?? throw new ClientNotFoundException(id);

            return ToDto(client);
        }

        public async Task<PagedResultDto<ClientDto>> GetAllAsync(int pageNumber, int pageSize)
        {
            var (items, total) = await _repository.GetAllAsync(pageNumber, pageSize);
            return new PagedResultDto<ClientDto>(items.Select(ToDto).ToList(), total, pageNumber, pageSize);
        }

        public async Task<PagedResultDto<ClientDto>> GetPagedByTypeAsync(ClientType type, int pageNumber, int pageSize)
        {
            var (items, total) = await _repository.GetPagedByTypeAsync(type, pageNumber, pageSize);
            return new PagedResultDto<ClientDto>(items.Select(ToDto).ToList(), total, pageNumber, pageSize);
        }

        public async Task<PagedResultDto<ClientDto>> GetPagedDeletedAsync(int pageNumber, int pageSize)
        {
            var (items, total) = await _repository.GetPagedDeletedAsync(pageNumber, pageSize);
            return new PagedResultDto<ClientDto>(items.Select(ToDto).ToList(), total, pageNumber, pageSize);
        }

        public async Task<ClientStatsDto> GetStatsAsync()
        {
            return await _repository.GetStatsAsync();
        }

        public async Task<ClientDto> CreateAsync(CreateClientRequestDto dto)
        {
            var client = new Client(
                dto.Type!.Value,
                dto.Name,
                dto.Email,
                dto.Address,
                dto.Phone,
                dto.TaxNumber
            );
            await _repository.AddAsync(client);
            await _repository.SaveChangesAsync();

            return ToDto(client);
        }

        public async Task<ClientDto> UpdateAsync(Guid id, UpdateClientRequestDto dto)
        {
            var client = await _repository.GetByIdAsync(id) ?? throw new ClientNotFoundException(id);


            client.Update(
                dto.Type!.Value,
                dto.Name,
                dto.Email,
                dto.Address,
                dto.Phone,
                dto.TaxNumber
            );
            await _repository.SaveChangesAsync();

            return ToDto(client);
        }

        public async Task DeleteAsync(Guid id)
        {
            var client = await _repository.GetByIdAsync(id) ?? throw new ClientNotFoundException(id);

            client.Delete();
            await _repository.SaveChangesAsync();
        }

        public async Task RestoreAsync(Guid id)
        {
            var client = await _repository.GetByIdDeletedAsync(id);

            if (client is null)
                throw new KeyNotFoundException($"Client with id {id} was not found.");

            client.Restore();
            await _repository.SaveChangesAsync();
        }

        private static ClientDto ToDto(Client client) => new(
            client.Id,
            client.Type.ToString(),
            client.Name,
            client.Email,
            client.Address,
            client.Phone,
            client.TaxNumber,
            client.IsDeleted,
            client.CreatedAt,
            client.UpdatedAt
        );
    }
}