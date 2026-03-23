using ERP.AuthService.Application.DTOs.AuthUser;
using ERP.AuthService.Application.DTOs.Role;

namespace ERP.AuthService.Application.Interfaces.Services
{
    public interface IControleService
    {
        Task<PagedResultDto<ControleResponseDto>> GetAllAsync(int pageNum, int pageSize);
        Task<PagedResultDto<ControleResponseDto>> GetByCategoryAsync(string category, int pageNum, int pageSize);
        Task<ControleResponseDto> GetByIdAsync(Guid id);
        Task<ControleResponseDto> CreateControleAsync(ControleRequestDto dto, Guid requesterId);
        Task<ControleResponseDto> UpdateControleAsync(Guid id,ControleRequestDto dto ,Guid requesterId);
        Task DeleteByIdAsync(Guid id, Guid requesterId);
        Task DeleteAllAsync();
    }
}
