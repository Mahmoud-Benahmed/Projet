namespace ERP.PaymentService.Infrastructure.Messaging
{
    public interface IEventPublisher
    {
        Task PublishAsync<T>(string topic, T message);
    }
}
