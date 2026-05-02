namespace ERP.InvoiceService.Application.DTOs;

public static class RegexPatterns
{
    public const string SafeText = @"^[\p{L}0-9\s,.'\-]+$";
    public const string Phone = @"^\+?\d{8,15}$";
    public const string AlphaNumeric = @"^[A-Za-z0-9]+$";
}