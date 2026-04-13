namespace ERP.ClientService.Infrastructure.Messaging
{
    public interface IEventPublisher
    {
        Task PublishAsync<T>(string topic, T @event);
    }
}
