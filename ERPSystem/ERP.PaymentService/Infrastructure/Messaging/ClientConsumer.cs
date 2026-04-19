using Confluent.Kafka;
using ERP.PaymentService.Application.Interfaces;
using ERP.PaymentService.Domain.LocalCache;
using ERP.PaymentService.Infrastructure.Messaging.Events.ClientEvents;
using System.Text.Json;

namespace ERP.PaymentService.Infrastructure.Messaging
{
    public class ClientConsumer : BackgroundService
    {
        private readonly IConsumer<string, string> _consumer;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ClientConsumer> _logger;

        public ClientConsumer(
            ILogger<ClientConsumer> logger,
            IConsumer<string, string> consumer,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _consumer = consumer;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _consumer.Subscribe(PaymentTopics.ClientUpdated);

            _logger.LogInformation(
                "\n\nClientConsumer started. Listening to topic: {Topic}\n\n",
                PaymentTopics.ClientUpdated);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = _consumer.Consume(stoppingToken);

                    if (result?.Message?.Value is null)
                        continue;

                    _logger.LogInformation(
                        "\n\nReceived message on topic '{Topic}'. Payload={Payload}\n\n",
                        result.Topic, result.Message.Value);

                    var ev = JsonSerializer.Deserialize<ClientUpdatedEvent>(result.Message.Value);

                    if (ev is null)
                    {
                        _logger.LogWarning("\n\nFailed to deserialize ClientUpdatedEvent\n\n");
                        continue;
                    }

                    var client = new Client
                    {
                        ClientId = ev.ClientId,
                        Name = ev.Name,
                        DelaiRetour = ev.DelaiRetour,
                        IsBlocked = ev.IsBlocked,
                        IsDeleted = ev.IsDeleted
                    };

                    using var scope = _scopeFactory.CreateScope();
                    var clientCacheRepository = scope.ServiceProvider.GetRequiredService<IClientCacheRepository>();

                    await clientCacheRepository.UpsertAsync(client);

                    _logger.LogInformation(
                        "\n\nClient cache upserted for ClientId={ClientId}\n\n",
                        client.ClientId);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("\n\nClientConsumer shutting down\n\n");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "\n\nError processing client event\n\n");
                    await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
                }
            }

            _consumer.Close();
        }
    }
}
