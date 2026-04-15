namespace ERP.InvoiceService.Infrastructure.Messaging.Events
{
    public interface IEventPublisher
    {
        Task PublishAsync<T>(string topic, T @event);
    }
}
