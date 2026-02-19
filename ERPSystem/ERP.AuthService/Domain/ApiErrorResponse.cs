namespace ERP.AuthService.Domain
{
    public class ApiErrorResponse
    {
        public string Code { get; set; } = default!;
        public string Message { get; set; } = default!;
    }
}
