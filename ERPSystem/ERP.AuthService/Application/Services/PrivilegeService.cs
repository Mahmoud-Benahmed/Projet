using ERP.AuthService.Application.DTOs.Role;
using ERP.AuthService.Application.Exceptions.Role;
using ERP.AuthService.Application.Interfaces.Repositories;
using ERP.AuthService.Application.Interfaces.Services;

namespace ERP.AuthService.Application.Services
{
    public class PrivilegeService : IPrivilegeService
    {
        private readonly IPrivilegeRepository _privilegeRepository;
        private readonly IControleRepository _controleRepository;
        private readonly IRoleRepository _roleRepository;

        public PrivilegeService(
            IPrivilegeRepository privilegeRepository,
            IControleRepository controleRepository,
            IRoleRepository roleRepository)
        {
            _privilegeRepository = privilegeRepository;
            _controleRepository = controleRepository;
            _roleRepository = roleRepository;
        }

        public async Task<List<PrivilegeResponseDto>> GetByRoleIdAsync(Guid roleId)
        {
            _ = await _roleRepository.GetByIdAsync(roleId)
                ?? throw new RoleNotFoundException(roleId);

            var privileges = await _privilegeRepository.GetByRoleIdAsync(roleId);
            var result = new List<PrivilegeResponseDto>();

            foreach (var p in privileges)
            {
                var controle = await _controleRepository.GetByIdAsync(p.ControleId)
                               ?? throw new ControleNotFoundException(p.ControleId);

                result.Add(new PrivilegeResponseDto(
                    p.Id,
                    p.RoleId,
                    p.ControleId,
                    controle.Libelle,
                    controle.Category,
                    p.IsGranted));
            }

            return result;
        }

        public async Task AllowAsync(Guid roleId, Guid controleId)
        {
            var privilege = await _privilegeRepository
                                .GetByRoleIdAndControleIdAsync(roleId, controleId)
                            ?? throw new PrivilegeNotFoundException(roleId, controleId);

            privilege.SetGranted(true);
            await _privilegeRepository.UpdateAsync(privilege);
        }

        public async Task DenyAsync(Guid roleId, Guid controleId)
        {
            var privilege = await _privilegeRepository
                                .GetByRoleIdAndControleIdAsync(roleId, controleId)
                            ?? throw new PrivilegeNotFoundException(roleId, controleId);

            privilege.SetGranted(false);
            await _privilegeRepository.UpdateAsync(privilege);
        }
    }
}