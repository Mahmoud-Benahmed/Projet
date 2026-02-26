using ERP.AuthService.Application.DTOs.Role;

namespace ERP.AuthService.Application.Interfaces.Services
{
    public interface IRoleService
    {
        Task<List<RoleResponseDto>> GetAllAsync();
        Task<RoleResponseDto> GetByIdAsync(Guid id);
    }
}