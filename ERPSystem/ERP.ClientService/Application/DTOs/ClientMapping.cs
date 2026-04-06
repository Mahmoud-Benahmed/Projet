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
            DuePaymentPeriod: client.GetEffectiveDuePaymentPeriod(),
            Phone: client.Phone,
            TaxNumber: client.TaxNumber,
            CreditLimit: client.GetEffectiveCreditLimit(),
            DelaiRetour: client.GetEffectiveDelaiRetour(),
            IsBlocked: client.IsBlocked,
            IsDeleted: client.IsDeleted,
            CreatedAt: client.CreatedAt,
            UpdatedAt: client.UpdatedAt,
            Categories: (client.ClientCategories ?? new List<ClientCategory>())
                                    .Select(cc => new ClientCategoryResponseDto(
                                        Id: cc.CategoryId,
                                        Name: cc.Category?.Name ?? string.Empty,
                                        Code: cc.Category?.Code ?? string.Empty,
                                        AssignedAt: cc.AssignedAt))
                                    .ToList()
        );
}