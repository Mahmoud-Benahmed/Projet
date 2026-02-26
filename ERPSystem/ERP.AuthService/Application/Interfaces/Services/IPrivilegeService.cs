using ERP.AuthService.Application.DTOs.Role;

namespace ERP.AuthService.Application.Interfaces.Services
{
    public interface IPrivilegeService
    {
        Task<List<PrivilegeResponseDto>> GetByRoleIdAsync(Guid roleId);
        Task AllowAsync(Guid roleId, Guid controleId);
        Task DenyAsync(Guid roleId, Guid controleId);
    }
}
