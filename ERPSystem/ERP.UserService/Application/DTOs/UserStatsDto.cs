namespace ERP.UserService.Application.DTOs
{
    public class UserStatsDto
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int DeactivatedUsers { get; set; }
        public int CompletedProfiles { get; set; }
    }
}
