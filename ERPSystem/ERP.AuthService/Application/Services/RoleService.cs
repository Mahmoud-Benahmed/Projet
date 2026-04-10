using ERP.AuthService.Application.DTOs.AuthUser;
using ERP.AuthService.Application.DTOs.Role;
using ERP.AuthService.Application.Exceptions;
using ERP.AuthService.Application.Interfaces.Repositories;
using ERP.AuthService.Application.Interfaces.Services;
using ERP.AuthService.Domain;
using ERP.AuthService.Domain.Logger;

namespace ERP.AuthService.Application.Services
{
    public class RoleService : IRoleService
    {
        private readonly IAuditLogger _auditLogger;
        private readonly IRoleRepository _roleRepository;

        public RoleService(IAuditLogger auditLogger, IRoleRepository roleRepository)
        {
            _auditLogger = auditLogger;
            _roleRepository = roleRepository;
        }

        public async Task<PagedResultDto<RoleResponseDto>> GetAllPagedAsync(int pageNumber, int pageSize)
        {
            ValidatePaging(pageNumber, pageSize);
            var (items, totalCount) = await _roleRepository.GetAllPagedAsync(pageNumber, pageSize);
            var mapped = items.Select(MapToDto).ToList();
            return new PagedResultDto<RoleResponseDto>(
                mapped,
                totalCount,
                pageNumber,
                pageSize);

        }


        public async Task<List<RoleResponseDto>> GetAllAsync()
        {
            var items = await _roleRepository.GetAllAsync();
            return items.Select(MapToDto).ToList();
        }

        public async Task<RoleResponseDto> GetByIdAsync(Guid id)
        {
            var role = await _roleRepository.GetByIdAsync(id)
                       ?? throw new RoleNotFoundException(id);
            return new RoleResponseDto(role.Id, role.Libelle);
        }

        public async Task<RoleResponseDto> CreateRole(RoleCreateDto dto, Guid performedById)
        {
            var existing = await _roleRepository.GetByLibelleAsync(dto.Libelle);
            if (existing is not null)
                throw new RoleAlreadyExistException(dto.Libelle);
            var newRole = new Role(dto.Libelle);

            await _roleRepository.AddAsync(newRole);

            await _auditLogger.LogAsync(
                    AuditAction.RoleCreated,
                    success: true,
                    performedBy: performedById,
                    metadata: new() { ["created"] = dto.Libelle.ToString(), ["createdBy"] = performedById.ToString() });

            return new RoleResponseDto(newRole.Id, newRole.Libelle);
        }
        public async Task<RoleResponseDto> UpdateAsync(Guid id, RoleUpdateDto dto, Guid performedById)
        {
            var role = await _roleRepository.GetByIdAsync(id) ?? throw new RoleNotFoundException(id);
            var before = role.Libelle.ToString();

            role.UpdateRole(dto.Libelle);
            await _roleRepository.UpdateAsync(role);

            await _auditLogger.LogAsync(
                    AuditAction.RoleUpdated,
                    success: true,
                    performedBy: performedById,
                    metadata: new()
                    {
                        ["before"] = before,
                        ["after"] = dto.Libelle.ToString(),
                        ["performedBy"] = performedById.ToString()
                    });

            return new RoleResponseDto(role.Id, role.Libelle);
        }

        public async Task DeleteAsync(Guid id, Guid performedById)
        {
            var role = await _roleRepository.GetByIdAsync(id) ?? throw new RoleNotFoundException(id);
            await _roleRepository.DeleteAsync(id);
            await _auditLogger.LogAsync(
                    AuditAction.RoleDeleted,
                    success: true,
                    performedBy: performedById,
                    metadata: new() { ["deleted"] = role.Libelle.ToString(), ["performedBy"] = performedById.ToString() });

        }

        private static RoleResponseDto MapToDto(Role role) =>
            new(
                role.Id,
                role.Libelle
            );

        private static void ValidatePaging(int pageNumber, int pageSize)
        {
            if (pageNumber < 1)
                throw new ArgumentOutOfRangeException(nameof(pageNumber),
                    "Page number must be greater than zero.");
            if (pageSize < 1)
                throw new ArgumentOutOfRangeException(nameof(pageSize),
                    "Page size must be greater than zero.");
        }


    }
}