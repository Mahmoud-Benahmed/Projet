using System.ComponentModel.DataAnnotations;

namespace ERP.ArticleService.Application.DTOs
{
    public record CreateArticleRequestDto(
        [Required(ErrorMessage = "Libelle is required.")]
        [MaxLength(200, ErrorMessage = "Libelle cannot exceed 200 characters.")]
        string Libelle,

        [Required(ErrorMessage = "Prix is required.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Prix must be greater than zero.")]
        decimal Prix,

        [Required(ErrorMessage = "CategoryId is required.")]
        Guid CategoryId,

        [Required(ErrorMessage = "BarCode is required.")]
        [StringLength(13, MinimumLength = 8, ErrorMessage = "BarCode must be between 8 and 13 characters.")]
        string BarCode,

        [Range(0.01, 100, ErrorMessage = "TVA must be between 0.01 and 100.")]
        decimal? TVA
    );

    public record UpdateArticleRequestDto(
        [Required(ErrorMessage = "Libelle is required.")]
        [MaxLength(200, ErrorMessage = "Libelle cannot exceed 200 characters.")]
        string Libelle,

        [Required(ErrorMessage = "Prix is required.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Prix must be greater than zero.")]
        decimal Prix,

        [Required(ErrorMessage = "CategoryId is required.")]
        Guid CategoryId,

        [StringLength(13, MinimumLength = 8, ErrorMessage = "BarCode must be between 8 and 13 characters.")]
        string? BarCode,

        [Range(0.01, 100, ErrorMessage = "TVA must be between 0.01 and 100.")]
        decimal? TVA
    );
}
