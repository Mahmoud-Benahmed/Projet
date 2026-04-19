namespace ERP.PaymentService.Application.Interfaces
{
    public interface IKafkaEventPublisher
    {
        Task PublishAsync<T>(string topic, T message);
    }
}
