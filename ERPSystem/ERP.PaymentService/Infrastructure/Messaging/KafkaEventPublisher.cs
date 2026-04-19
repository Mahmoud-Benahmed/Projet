using Confluent.Kafka;
using ERP.PaymentService.Application.Interfaces;
using System.Text.Json;

namespace ERP.PaymentService.Infrastructure.Messaging
{
    public class KafkaEventPublisher : IKafkaEventPublisher
    {
        private readonly IProducer<string, string> _producer;
        private readonly ILogger<KafkaEventPublisher> _logger;

        public KafkaEventPublisher(
            ILogger<KafkaEventPublisher> logger,
            IProducer<string, string> producer)
        {
            _logger = logger;
            _producer = producer;
        }

        public async Task PublishAsync<T>(string topic, T message)
        {
            var json = JsonSerializer.Serialize(message);

            _logger.LogInformation(
                "\n\nPublishing event to topic '{Topic}'. Payload={Payload}\n\n",
                topic, json);

            var kafkaMessage = new Message<string, string>
            {
                Key = Guid.NewGuid().ToString(),
                Value = json
            };

            var result = await _producer.ProduceAsync(topic, kafkaMessage);

            _logger.LogInformation(
                "\n\nEvent published to topic '{Topic}' at partition {Partition}, offset {Offset}\n\n",
                topic, result.Partition.Value, result.Offset.Value);
        }
    }
}
