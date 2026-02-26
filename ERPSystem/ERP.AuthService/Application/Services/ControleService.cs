using ERP.AuthService.Application.DTOs.Role;
using ERP.AuthService.Application.Exceptions.Role;
using ERP.AuthService.Application.Interfaces.Repositories;
using ERP.AuthService.Application.Interfaces.Services;

namespace ERP.AuthService.Application.Services
{
    public class ControleService : IControleService
    {
        private readonly IControleRepository _controleRepository;

        public ControleService(IControleRepository controleRepository)
        {
            _controleRepository = controleRepository;
        }

        public async Task<List<ControleResponseDto>> GetAllAsync()
        {
            var controles = await _controleRepository.GetAllAsync();
            return controles.Select(MapToDto).ToList();
        }

        public async Task<ControleResponseDto> GetByIdAsync(Guid id)
        {
            var controle = await _controleRepository.GetByIdAsync(id)
                           ?? throw new ControleNotFoundException(id);
            return MapToDto(controle);
        }

        public async Task<List<ControleResponseDto>> GetByCategoryAsync(string category)
        {
            var controles = await _controleRepository.GetByCategoryAsync(category) ?? throw new ControleNotFoundException($"No Controle belong to the given Category '{category}'");
            return controles.Select(MapToDto).ToList();
        }

        private static ControleResponseDto MapToDto(Domain.Controle c) =>
            new(c.Id, c.Category, c.Libelle, c.Description);
    }
}