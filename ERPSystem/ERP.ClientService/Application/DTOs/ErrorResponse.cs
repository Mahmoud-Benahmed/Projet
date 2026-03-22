namespace ERP.ClientService.Application.DTOs
{
    public record ErrorResponse
    {
        public string Code { get; set; }
        public string Message { get; set; }
        public int StatusCode { get; set; }
    }
}
