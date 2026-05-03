namespace ERP.TenantService.Infrastructure.Messaging;

public class KafkaEventPublisher : IEventPublisher
{
    private readonly ILogger<KafkaEventPublisher> _logger;

    public KafkaEventPublisher(ILogger<KafkaEventPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync(string topic, object payload)
    {
        // Stub: wire up Confluent.Kafka producer here when ready
        _logger.LogInformation("[Kafka Stub] Publishing to topic '{Topic}': {@Payload}", topic, payload);
        return Task.CompletedTask;
    }
}
