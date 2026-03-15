using ERP.ClientService.Domain;
using System.ComponentModel.DataAnnotations;
using ERP.ClientService.Application.Validation;

namespace ERP.ClientService.Application.DTOs
{
    public record ClientDto(
        Guid Id,
        string Type,
        string Name,
        string Email,
        string Address,
        string? Phone,
        string? TaxNumber,
        bool IsDeleted,
        DateTime CreatedAt,
        DateTime? UpdatedAt
    );

    [CompanyRequiresTaxNumber]
    public record CreateClientRequestDto(
        [Required(ErrorMessage = "Type is required.")]
        [EnumDataType(typeof(ClientType), ErrorMessage = "Type is not valid.")]
        ClientType? Type,

        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(200, ErrorMessage = "Name cannot exceed 200 characters.")]
        string Name,

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Email is not valid.")]
        [MaxLength(250, ErrorMessage = "Email cannot exceed 250 characters.")]
        string Email,

        [Required(ErrorMessage = "Address is required.")]
        [MaxLength(500, ErrorMessage = "Address cannot exceed 500 characters.")]
        string Address,

        [Phone(ErrorMessage = "Phone number is not valid.")]
        [MaxLength(50, ErrorMessage = "Phone cannot exceed 50 characters.")]
        string? Phone,

        [MaxLength(100, ErrorMessage = "Tax number cannot exceed 100 characters.")]
        string? TaxNumber
    );

    [CompanyRequiresTaxNumber]
    public record UpdateClientRequestDto(
        [Required(ErrorMessage = "Type is required.")]
        [EnumDataType(typeof(ClientType), ErrorMessage = "Type is not valid.")]
        ClientType? Type,

        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(200, ErrorMessage = "Name cannot exceed 200 characters.")]
        string Name,

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Email is not valid.")]
        [MaxLength(250, ErrorMessage = "Email cannot exceed 250 characters.")]
        string Email,

        [Required(ErrorMessage = "Address is required.")]
        [MaxLength(500, ErrorMessage = "Address cannot exceed 500 characters.")]
        string Address,

        [Phone(ErrorMessage = "Phone number is not valid.")]
        [MaxLength(50, ErrorMessage = "Phone cannot exceed 50 characters.")]
        string? Phone,

        [MaxLength(100, ErrorMessage = "Tax number cannot exceed 100 characters.")]
        string? TaxNumber
    );

    public record ClientStatsDto(
        int TotalCount,
        int ActiveCount,
        int DeletedCount
    );
}