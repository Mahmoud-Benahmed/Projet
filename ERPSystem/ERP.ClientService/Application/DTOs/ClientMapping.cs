using ERP.ClientService.Application.DTOs;
using ERP.ClientService.Domain;

namespace ERP.ClientService.Application.DTOs;

public static class ClientMappings
{
    public static ClientResponseDto ToResponseDto(this Client client) =>
        new(
            Id: client.Id,
            Name: client.Name,
            Email: client.Email,
            Address: client.Address,
            Phone: client.Phone,
            TaxNumber: client.TaxNumber,
            CreditLimit: client.CreditLimit,
            DelaiRetour: client.DelaiRetour,
            IsBlocked: client.IsBlocked,
            IsDeleted: client.IsDeleted,
            CreatedAt: client.CreatedAt,
            UpdatedAt: client.UpdatedAt,
            Categories: client.ClientCategories
                               .Select(cc => new ClientCategoryResponseDto(
                                   CategoryId: cc.CategoryId,
                                   CategoryName: cc.Category?.Name ?? string.Empty,
                                   CategoryCode: cc.Category?.Code ?? string.Empty,
                                   AssignedAt: cc.AssignedAt))
                               .ToList()
        );
}