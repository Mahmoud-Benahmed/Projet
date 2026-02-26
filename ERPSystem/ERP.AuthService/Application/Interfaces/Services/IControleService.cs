using ERP.AuthService.Application.DTOs.Role;

namespace ERP.AuthService.Application.Interfaces.Services
{
    public interface IControleService
    {
        Task<List<ControleResponseDto>> GetAllAsync();
        Task<ControleResponseDto> GetByIdAsync(Guid id);
        Task<List<ControleResponseDto>> GetByCategoryAsync(string category);
    }
}
