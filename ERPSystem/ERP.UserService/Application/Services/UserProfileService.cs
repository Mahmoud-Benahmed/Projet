namespace ERP.UserService.Application.Services;

using ERP.UserService.Application.DTOs;
using ERP.UserService.Application.Exceptions;
using ERP.UserService.Application.Interfaces;
using ERP.UserService.Domain;

public class UserProfileService : IUserProfileService
{
    private readonly IUserProfileRepository _repository;

    public UserProfileService(IUserProfileRepository repository)
    {
        _repository = repository;
    }

    // =========================
    // CREATE
    // =========================
    public async Task<UserProfileResponseDto> CreateProfileAsync(CreateUserProfileDto dto)
    {
        var existsByLogin = await _repository.ExistsByLoginAsync(dto.Login);
        if (existsByLogin)
            throw new UserProfileAlreadyExistsException(dto.Login);

        var existing = await _repository.GetByAuthUserIdAsync(dto.AuthUserId);
        if (existing != null)
            throw new UserProfileAlreadyExistsException(dto.AuthUserId);

        var existsByEmail = await _repository.ExistsByEmailAsync(dto.Email);
        if (existsByEmail)
            throw new UserProfileAlreadyExistsException(dto.Email);

        var profile = new UserProfile(dto.Login, dto.Role, dto.AuthUserId, dto.Email);

        await _repository.AddAsync(profile);
        await _repository.SaveChangesAsync();

        return MapToDto(profile);
    }



    // =========================
    // READ - BY ID
    // =========================
    public async Task<UserProfileResponseDto> GetByIdAsync(Guid id)
    {
        var profile = await _repository.GetByIdAsync(id)
                      ?? throw new UserProfileNotFoundException(id);

        return MapToDto(profile);
    }



    // =========================
    // READ - BY AUTH USER ID
    // =========================
    public async Task<UserProfileResponseDto> GetByAuthUserIdAsync(Guid authUserId)
    {
        var profile = await _repository.GetByAuthUserIdAsync(authUserId)
                      ?? throw new UserProfileNotFoundException(
                          $"Profile with AuthUserId '{authUserId}' not found.");

        return MapToDto(profile);
    }

    // =========================
    // READ - BY LOGIN
    // =========================
    public async Task<UserProfileResponseDto> GetByLoginAsync(string login)
    {
        var profile = await _repository.GetByLoginAsync(login) ?? throw new UserProfileNotFoundException($"User profile with Login: {login} not found");
        return MapToDto(profile);
    }

    // =========================
    // EXISTS - BY AUTH USER ID
    // =========================
    public async Task<bool> ExistsByAuthUserIdAsync(Guid authUserId)
    {
        return await _repository.ExistsByAuthUserIdAsync(authUserId);
    }


    // =========================
    // EXISTS - BY LOGIN
    // =========================
    public async Task<bool> ExistsByLoginAsync(string login)
    {
        return await _repository.ExistsByLoginAsync(login);
    }


    // =========================
    // READ - ALL
    // =========================
    public async Task<List<UserProfileResponseDto>> GetAllAsync()
    {
        var profiles = await _repository.GetAllAsync();

        return profiles
            .Select(MapToDto)
            .ToList();
    }


    // =========================
    // GET BY STATUS: ACTIVE/ DEACTIVE
    // =========================
    public async Task<PagedResultDto<UserProfileResponseDto>>
    GetPagedByStatusAsync(bool isActive, int pageNumber, int pageSize)
    {
        var (items, totalCount) =
            await _repository.GetPagedByStatusAsync(isActive, pageNumber, pageSize);

        var mapped = items.Select(MapToDto).ToList();

        return new PagedResultDto<UserProfileResponseDto>(
            mapped,
            totalCount,
            pageNumber,
            pageSize);
    }



    // =========================
    // GET BY ROLE PAGED
    // =========================
    public async Task<PagedResultDto<UserProfileResponseDto>>
    GetPagedByRoleAsync(string role, int pageNumber, int pageSize)
    {
        var (items, totalCount) =
            await _repository.GetPagedByRoleAsync(role, pageNumber, pageSize);

        var mapped = items.Select(MapToDto).ToList();

        return new PagedResultDto<UserProfileResponseDto>(
            mapped,
            totalCount,
            pageNumber,
            pageSize);
    }






    // =========================
    // UPDATE - COMPLETE PROFILE
    // =========================
    public async Task<UserProfileResponseDto> CompleteProfileAsync(Guid authUserId, CompleteProfileDto dto)
    {
        var profile = await _repository.GetByAuthUserIdAsync(authUserId)
                      ?? throw new UserProfileNotFoundException(
                          $"Profile with AuthUserId '{authUserId}' not found.");

        profile.CompleteProfile(dto.FullName, dto.Phone);

        await _repository.SaveChangesAsync();

        return MapToDto(profile);
    }


    // =========================
    // UPDATE - ACTIVATE
    // =========================
    public async Task ActivateAsync(Guid id)
    {
        var profile = await _repository.GetByIdAsync(id)
                      ?? throw new UserProfileNotFoundException(id);

        profile.Activate();

        await _repository.SaveChangesAsync();
    }



    // =========================
    // UPDATE - DEACTIVATE
    // =========================
    public async Task DeactivateAsync(Guid id)
    {
        var profile = await _repository.GetByIdAsync(id)
                      ?? throw new UserProfileNotFoundException(id);

        profile.Deactivate();

        await _repository.SaveChangesAsync();
    }



    // =========================
    // DELETE (Soft delete recommended)
    // =========================
    public async Task DeleteAsync(Guid id)
    {
        var profile = await _repository.GetByIdAsync(id)
                      ?? throw new UserProfileNotFoundException(id);

        _repository.Remove(profile);

        await _repository.SaveChangesAsync();
    }



    // =========================
    // STATS
    // =========================
    public async Task<UserStatsDto> GetStatsAsync()
    {
        return await _repository.GetStatsAsync();
    }



    // =========================
    // MAPPING
    // =========================
    private static UserProfileResponseDto MapToDto(UserProfile profile)
    {
        return new UserProfileResponseDto
        {
            Id = profile.Id,
            AuthUserId = profile.AuthUserId,
            Login = profile.Login,
            Email = profile.Email,
            FullName = profile.FullName,
            Phone = profile.Phone,
            Role = profile.Role,
            IsActive = profile.IsActive,
            IsProfileCompleted = profile.IsProfileCompleted(),
            CreatedAt = profile.CreatedAt,
            UpdatedAt = profile.UpdatedAt
        };
    }
}