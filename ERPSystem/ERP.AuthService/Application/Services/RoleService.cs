using ERP.AuthService.Application.DTOs.Role;
using ERP.AuthService.Application.Exceptions.Role;
using ERP.AuthService.Application.Interfaces.Repositories;
using ERP.AuthService.Application.Interfaces.Services;

namespace ERP.AuthService.Application.Services
{
    public class RoleService : IRoleService
    {
        private readonly IRoleRepository _roleRepository;

        public RoleService(IRoleRepository roleRepository)
        {
            _roleRepository = roleRepository;
        }

        public async Task<List<RoleResponseDto>> GetAllAsync()
        {
            var roles = await _roleRepository.GetAllAsync();
            return roles.Select(r => new RoleResponseDto(r.Id, r.Libelle)).ToList();
        }

        public async Task<RoleResponseDto> GetByIdAsync(Guid id)
        {
            var role = await _roleRepository.GetByIdAsync(id)
                       ?? throw new RoleNotFoundException(id);
            return new RoleResponseDto(role.Id, role.Libelle);
        }
    }
}