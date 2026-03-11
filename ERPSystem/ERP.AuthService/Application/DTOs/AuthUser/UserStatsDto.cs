namespace ERP.AuthService.Application.DTOs.AuthUser
{
    public record UserStatsDto
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int DeactivatedUsers { get; set; }
        public int DeletedUsers { get; set; }
    }
}
