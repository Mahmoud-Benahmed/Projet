namespace ERP.UserService.Application.Interfaces
{
    public interface IEventPublisher
    {
        Task PublishAsync<T>(string topic, T @event);
    }
}
