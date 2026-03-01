using Confluent.Kafka;
using ERP.UserService.Application.Interfaces;
using System.Text.Json;

namespace ERP.UserService.Infrastructure.Messaging
{
    public class KafkaEventPublisher : IEventPublisher, IDisposable
    {
        private readonly IProducer<string, string> _producer;

        public KafkaEventPublisher(IConfiguration configuration)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = configuration["Kafka:BootstrapServers"]
                    ?? throw new InvalidOperationException("Kafka:BootstrapServers not configured.")
            };
            _producer = new ProducerBuilder<string, string>(config).Build();
        }

        public async Task PublishAsync<T>(string topic, T @event)
        {
            var message = new Message<string, string>
            {
                Key = Guid.NewGuid().ToString(),
                Value = JsonSerializer.Serialize(@event)
            };
            await _producer.ProduceAsync(topic, message);
        }

        public void Dispose()
        {
            _producer.Flush(TimeSpan.FromSeconds(5));
            _producer.Dispose();
        }
    }
}