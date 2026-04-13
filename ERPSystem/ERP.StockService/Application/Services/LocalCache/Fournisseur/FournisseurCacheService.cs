// Application/Services/LocalCache/FournisseurCacheService.cs
using ERP.StockService.Application.DTOs;
using ERP.StockService.Application.Interfaces;
using ERP.StockService.Domain.LocalCache.Fournisseur;
using Microsoft.EntityFrameworkCore;

namespace ERP.StockService.Application.Services.LocalCache.Fournisseur;

public class FournisseurCacheService : IFournisseurCacheService
{
    private readonly IFournisseurCacheRepository _repository;
    private readonly ILogger<FournisseurCacheService> _logger;

    public FournisseurCacheService(
        IFournisseurCacheRepository repository,
        ILogger<FournisseurCacheService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    // =========================
    // READ OPERATIONS
    // =========================

    public async Task<FournisseurResponseDto?> GetByIdAsync(Guid id)
    {
        try
        {
            var fournisseur = await _repository.GetByIdAsync(id);
            return fournisseur != null ? MapToDto(fournisseur) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting fournisseur by ID {FournisseurId}", id);
            throw;
        }
    }

    public async Task<FournisseurResponseDto?> GetByNameAsync(string name)
    {
        try
        {
            var fournisseur = await _repository.GetByNameAsync(name);
            return fournisseur != null ? MapToDto(fournisseur) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting fournisseur by name {FournisseurName}", name);
            throw;
        }
    }

    public async Task<FournisseurResponseDto?> GetByTaxNumberAsync(string taxNumber)
    {
        try
        {
            var fournisseur = await _repository.GetByTaxNumberAsync(taxNumber);
            return fournisseur != null ? MapToDto(fournisseur) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting fournisseur by tax number {TaxNumber}", taxNumber);
            throw;
        }
    }

    public async Task<List<FournisseurResponseDto>> GetAllAsync()
    {
        try
        {
            var fournisseurs = await _repository.GetAllAsync();
            return fournisseurs.Select(MapToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all fournisseurs");
            throw;
        }
    }

    public async Task<List<FournisseurResponseDto>> GetActiveAsync()
    {
        try
        {
            var fournisseurs = await _repository.GetActiveAsync();
            return fournisseurs.Select(MapToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active fournisseurs");
            throw;
        }
    }

    public async Task<List<FournisseurResponseDto>> GetBlockedAsync()
    {
        try
        {
            var fournisseurs = await _repository.GetBlockedAsync();
            return fournisseurs.Select(MapToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting blocked fournisseurs");
            throw;
        }
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        try
        {
            return await _repository.ExistsAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking existence for fournisseur {FournisseurId}", id);
            throw;
        }
    }

    // =========================
    // SYNC OPERATIONS
    // =========================

    public async Task SyncCreatedAsync(FournisseurResponseDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            _logger.LogWarning("Fournisseur event has null or empty Name. Id: {FournisseurId}", dto.Id);
            return;
        }

        try
        {
            // Check if fournisseur already exists
            var existing = await _repository.GetByIdAsync(dto.Id);
            if (existing != null)
            {
                _logger.LogInformation("Fournisseur {FournisseurName} (Id: {FournisseurId}) already exists in cache. Updating instead.",
                    dto.Name, dto.Id);
                await SyncUpdatedAsync(dto);
                return;
            }

            // Check by name to prevent duplicates
            var existingByName = await _repository.GetByNameAsync(dto.Name);
            if (existingByName != null)
            {
                _logger.LogWarning(
                    "Fournisseur name '{FournisseurName}' already exists with different ID. Existing: {ExistingId}, New: {NewId}. Updating existing fournisseur.",
                    dto.Name, existingByName.Id, dto.Id);
                await SyncUpdatedAsync(dto);
                return;
            }

            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                var existingByEmail = await _repository.GetByEmailAsync(dto.Email);
                if (existingByEmail != null)
                {
                    _logger.LogWarning(
                        "Fournisseur email '{FournisseurEmail}' already exists with different ID. Existing: {ExistingId}, New: {NewId}. Updating existing fournisseur.",
                        dto.Email, existingByEmail.Id, dto.Id);
                    await SyncUpdatedAsync(dto);
                    return;
                }
            }

            // Check by tax number to prevent duplicates
            var existingByTax = await _repository.GetByTaxNumberAsync(dto.TaxNumber);
            if (existingByTax != null)
            {
                _logger.LogWarning(
                    "Fournisseur tax number '{TaxNumber}' already exists. Existing ID: {ExistingId}. Skipping creation.",
                    dto.TaxNumber, existingByTax.Id);
                return;
            }

            // Create new fournisseur
            var fournisseur = new FournisseurCache(dto);
            await _repository.AddAsync(fournisseur);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Fournisseur {FournisseurName} (Id: {FournisseurId}) added to cache",
                dto.Name, dto.Id);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("duplicate") == true)
        {
            _logger.LogWarning(ex, "Duplicate fournisseur detected for {FournisseurName}. This is expected if fournisseur already exists.",
                dto.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing created fournisseur {FournisseurName}", dto.Name);
            throw;
        }
    }

    public async Task SyncUpdatedAsync(FournisseurResponseDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        try
        {
            var existing = await _repository.GetByIdAsync(dto.Id);
            if (existing == null)
            {
                _logger.LogWarning("Fournisseur {FournisseurId} not found for update. Creating instead.", dto.Id);
                await SyncCreatedAsync(dto);
                return;
            }

            existing.ApplyUpdate(dto);
            await _repository.UpdateAsync(existing);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Fournisseur {FournisseurName} (Id: {FournisseurId}) updated in cache",
                dto.Name, dto.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing updated fournisseur {FournisseurName}", dto.Name);
            throw;
        }
    }

    public async Task SyncDeletedAsync(FournisseurResponseDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        try
        {
            var existing = await _repository.GetByIdAsync(dto.Id);
            if (existing == null)
            {
                _logger.LogWarning("Fournisseur {FournisseurId} not found for deletion", dto.Id);
                return;
            }

            existing.MarkDeleted();
            await _repository.UpdateAsync(existing);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Fournisseur {FournisseurName} (Id: {FournisseurId}) marked as deleted in cache",
                dto.Name, dto.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing deleted fournisseur {FournisseurId}", dto.Id);
            throw;
        }
    }

    public async Task SyncRestoredAsync(FournisseurResponseDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        try
        {
            var existing = await _repository.GetByIdAsync(dto.Id);
            if (existing == null)
            {
                _logger.LogWarning("Fournisseur {FournisseurId} not found for restore", dto.Id);
                return;
            }

            existing.MarkRestored();
            await _repository.UpdateAsync(existing);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Fournisseur {FournisseurName} (Id: {FournisseurId}) restored in cache",
                dto.Name, dto.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing restored fournisseur {FournisseurId}", dto.Id);
            throw;
        }
    }

    public async Task SyncBlockedAsync(Guid id, bool isBlocked)
    {
        try
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
            {
                _logger.LogWarning("Fournisseur {FournisseurId} not found for block/unblock operation", id);
                return;
            }

            if (isBlocked)
                existing.Block();
            else
                existing.Unblock();

            await _repository.UpdateAsync(existing);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Fournisseur {FournisseurName} (Id: {FournisseurId}) {Action} in cache",
                existing.Name, id, isBlocked ? "blocked" : "unblocked");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing blocked status for fournisseur {FournisseurId}", id);
            throw;
        }
    }

    // =========================
    // PRIVATE HELPERS
    // =========================

    private static FournisseurResponseDto MapToDto(FournisseurCache fournisseur)
    {
        return new FournisseurResponseDto(
            Id: fournisseur.Id,
            Name: fournisseur.Name,
            Address: fournisseur.Address,
            Phone: fournisseur.Phone,
            Email: fournisseur.Email,
            TaxNumber: fournisseur.TaxNumber,
            RIB: fournisseur.RIB,
            IsDeleted: fournisseur.IsDeleted,
            IsBlocked: fournisseur.IsBlocked,
            CreatedAt: fournisseur.CreatedAt,
            UpdatedAt: fournisseur.UpdatedAt
        );
    }
}