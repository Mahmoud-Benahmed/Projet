using ERP.AuthService.Application.DTOs.Role;
using ERP.AuthService.Application.Interfaces.Repositories;
using ERP.AuthService.Application.Interfaces.Services;
using ERP.AuthService.Domain;

namespace ERP.AuthService.Application.Services
{
    public class ControleService: IControleService
    {
        private readonly IControleRepository _controleRepository;

        public ControleService(IControleRepository controleRepository)
        {
            _controleRepository = controleRepository;
        }

        /// <summary>
        /// Get all controles.
        /// </summary>
        public async Task<List<ControleResponseDto>> GetAllAsync()
        {
            var controles = await _controleRepository.GetAllAsync();
            return controles.Select(MapToDto).ToList();
        }

        /// <summary>
        /// Get a controle by its ID.
        /// </summary>
        public async Task<ControleResponseDto?> GetByIdAsync(Guid id)
        {
            var controle = await _controleRepository.GetByIdAsync(id);
            return controle is null ? null : MapToDto(controle);
        }

        /// <summary>
        /// Get a controle by its Libelle.
        /// </summary>
        public async Task<ControleResponseDto?> GetByLibelleAsync(string libelle)
        {
            var controle = await _controleRepository.GetByLibelleAsync(libelle);
            return controle is null ? null : MapToDto(controle);
        }


        /// <summary>
        /// Get all controles belonging to a given category (case-insensitive).
        /// </summary>
        public async Task<List<ControleResponseDto>> GetByCategoryAsync(string category)
        {
            var controles = await _controleRepository.GetByCategoryAsync(category);
            return controles.Select(MapToDto).ToList();
        }

        /// <summary>
        /// Create a new controle.
        /// </summary>
        public async Task CreateControle(ControleRequestDto request)
        {
            var existing = await _controleRepository.GetByLibelleAsync(request.Libelle);
            if (existing is not null)
                throw new InvalidOperationException($"A controle with libelle '{request.Libelle}' already exists.");

            var controle = new Controle(request.Category, request.Libelle, request.Description);
            await _controleRepository.AddAsync(controle);
        }

        /// <summary>
        /// Update an existing controle.
        /// </summary>
        public async Task UpdateControle(Guid id, ControleRequestDto controle)
        {
            var existing = await _controleRepository.GetByIdAsync(id);
            if (existing is null)
                throw new KeyNotFoundException($"Controle with ID '{id}' was not found.");

            existing.Update(controle);

            await _controleRepository.UpdateAsync(existing);
        }

        /// <summary>
        /// Delete a controle by its ID.
        /// </summary>
        public async Task DeleteControle(Guid id)
        {
            var existing = await _controleRepository.GetByIdAsync(id);
            if (existing is null)
                throw new KeyNotFoundException($"Controle with ID '{id}' was not found.");

            await _controleRepository.DeleteAsync(id);
        }

        /// <summary>
        /// Delete all controles.
        /// </summary>
        public async Task DeleteAll()
        {
            await _controleRepository.DeleteAllAsync();
        }

        /// <summary>
        /// Get the total count of controles.
        /// </summary>
        public async Task<long> CountAsync()
        {
            return await _controleRepository.CountAsync();
        }

        // ── Mapping ───────────────────────────────────────────────────────────

        private static ControleResponseDto MapToDto(Controle controle) =>
            new(
                controle.Id,
                controle.Category,
                controle.Libelle,
                controle.Description
            );
    }
}