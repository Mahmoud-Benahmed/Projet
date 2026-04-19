namespace ERP.PaymentService.Domain.LocalCache
{
    public class Client
    {
        public Guid ClientId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int DelaiRetour { get; set; }
        public bool IsBlocked { get; set; }
        public bool IsDeleted { get; set; }
    }
}
