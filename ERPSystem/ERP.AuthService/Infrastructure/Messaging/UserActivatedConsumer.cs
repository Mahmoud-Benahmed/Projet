using Confluent.Kafka;
using ERP.AuthService.Application.Events;
using ERP.AuthService.Application.Exceptions.AuthUser;
using ERP.AuthService.Application.Interfaces.Services;
using System.Text.Json;

namespace ERP.AuthService.Infrastructure.Messaging
{
    public class UserActivatedConsumer : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UserActivatedConsumer> _logger;

        public UserActivatedConsumer(
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            ILogger<UserActivatedConsumer> logger)
        {
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = _configuration["Kafka:BootstrapServers"]
                    ?? throw new InvalidOperationException("Kafka:BootstrapServers not configured."),
                GroupId = "auth-service-group",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false
            };

            using var consumer = new ConsumerBuilder<string, string>(config).Build();
            consumer.Subscribe(Topics.UserActivated);

            _logger.LogInformation("UserActivatedConsumer started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(stoppingToken);
                    var @event = JsonSerializer.Deserialize<UserActivated>(result.Message.Value);

                    _logger.LogInformation("\nConsumed message: AuthUserId={Id}, Offset={Offset}\n",
                        @event?.AuthUserId, result.Offset);

                    if (@event is null)
                    {
                        _logger.LogWarning("Received null event, skipping.");
                        consumer.Commit(result);
                        continue;
                    }

                    using var scope = _scopeFactory.CreateScope();
                    var authUserService = scope.ServiceProvider
                        .GetRequiredService<IAuthUserService>();

                    try
                    {
                        await authUserService.ActivateAsync(Guid.Parse(@event.AuthUserId));
                    }
                    catch (UserNotFoundException ex)
                    {
                        _logger.LogWarning(
                            "AuthUser not found for AuthUserId: {Id}, skipping.", @event.AuthUserId);
                    }

                    consumer.Commit(result);
                    _logger.LogInformation(
                        "AuthUser activated for AuthUserId: {AuthUserId}", @event.AuthUserId);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error consuming user.activated event.");
                }
            }

            consumer.Close();
        }
    }
}