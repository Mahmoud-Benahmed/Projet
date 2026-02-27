using Confluent.Kafka;
using ERP.UserService.Application.DTOs;
using ERP.UserService.Application.Events;
using ERP.UserService.Application.Exceptions;
using ERP.UserService.Application.Interfaces;
using System.Text.Json;

namespace ERP.UserService.Infrastructure.Messaging
{
    public class UserRegisteredConsumer : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UserRegisteredConsumer> _logger;

        public UserRegisteredConsumer(
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            ILogger<UserRegisteredConsumer> logger)
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
                GroupId = "user-service-group",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false
            };

            using var consumer = new ConsumerBuilder<string, string>(config).Build();
            consumer.Subscribe(Topics.UserRegistered);

            _logger.LogInformation("UserRegisteredConsumer started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(stoppingToken);

                    var @event = JsonSerializer.Deserialize<UserRegisteredEvent>(result.Message.Value);

                    // ← add here
                    _logger.LogInformation("Consumed message: AuthUserId={Id}, Email={Email}, Offset={Offset}",
                                            @event?.AuthUserId, @event?.Email, result.Offset);

                    if (@event is null)
                    {
                        _logger.LogWarning("Received null event, skipping.");
                        continue;
                    }

                    using var scope = _scopeFactory.CreateScope();
                    var userProfileService = scope.ServiceProvider
                        .GetRequiredService<IUserProfileService>();

                    try
                    {
                        await userProfileService.CreateProfileAsync(new CreateUserProfileDto(
                            AuthUserId: Guid.Parse(@event.AuthUserId),
                            Email: @event.Email
                        ));
                    }
                    catch (UserProfileAlreadyExistsException)
                    {
                        // already processed, skip silently
                        _logger.LogWarning(
                            "Profile already exists for AuthUserId: {Id}, skipping.", @event.AuthUserId);
                    }

                    consumer.Commit(result); // always commit so offset advances

                    consumer.Commit(result);

                    _logger.LogInformation(
                        "Profile created for AuthUserId: {AuthUserId}", @event.AuthUserId);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error consuming user.registered event.");
                }
            }

            consumer.Close();
        }
    }
}