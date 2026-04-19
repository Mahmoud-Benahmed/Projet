namespace ERP.PaymentService.Infrastructure.Messaging.Events.ClientEvents
{
    public class ClientUpdatedEvent
    {
        public Guid ClientId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int DelaiRetour { get; set; }
        public bool IsBlocked { get; set; }
        public bool IsDeleted { get; set; }
    }
}
