using ERP.AuthService.Application.DTOs.AuthUser;
using ERP.AuthService.Application.DTOs.Role;

namespace ERP.AuthService.Application.Interfaces.Services
{
    public interface IRoleService
    {
        Task<PagedResultDto<RoleResponseDto>> GetAllAsync(int pageNumber, int pageSize);
        Task<RoleResponseDto> GetByIdAsync(Guid id);
        Task<RoleResponseDto> CreateRole(RoleCreateDto role, Guid performedById);
        Task<RoleResponseDto> UpdateAsync(Guid id, RoleUpdateDto role, Guid performedById);
        Task DeleteAsync(Guid id, Guid performedById);
    }
}