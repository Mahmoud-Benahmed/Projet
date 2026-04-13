using Confluent.Kafka;
using ERP.StockService.Application.DTOs;
using System.Text.Json;

namespace ERP.StockService.Infrastructure.Messaging.ArticleEvents.Category;

public sealed class CategoryEventConsumer : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CategoryEventConsumer> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public CategoryEventConsumer(
        IConfiguration configuration,
        IServiceScopeFactory scopeFactory,
        ILogger<CategoryEventConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        var config = new ConsumerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"]
                ?? throw new InvalidOperationException("Kafka:BootstrapServers not configured."),
            GroupId = "stock-service-category-cache",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            AllowAutoCreateTopics = true,
            SocketTimeoutMs = 60000,
            SessionTimeoutMs = 60000
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
        _consumer.Subscribe([CategoryTopics.Created, CategoryTopics.Updated, CategoryTopics.Deleted, CategoryTopics.Restored]);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Run(async () =>
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = _consumer.Consume(stoppingToken);

                    // Log raw message
                    _logger.LogInformation("Raw category message: {Raw}", result.Message.Value);

                    var dto = JsonSerializer.Deserialize<CategoryResponseDto>(
                        result.Message.Value, _jsonOptions);

                    if (dto is null)
                    {
                        _logger.LogWarning("Failed to deserialize category event");
                        _consumer.Commit(result);
                        continue;
                    }

                    _logger.LogInformation("Deserialized category - Id: {Id}, Name: '{Name}', TVA: {TVA}",
                        dto.Id, dto.Name ?? "NULL", dto.TVA);

                    if (string.IsNullOrWhiteSpace(dto.Name))
                    {
                        _logger.LogError("Category name is null or empty! Raw JSON: {Raw}", result.Message.Value);
                        _consumer.Commit(result);
                        continue;
                    }

                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var handler = scope.ServiceProvider.GetRequiredService<ICategoryEventHandler>();

                        switch (result.Topic)
                        {
                            case CategoryTopics.Created:
                                await handler.HandleCreatedAsync(dto);
                                break;
                            case CategoryTopics.Updated:
                                await handler.HandleUpdatedAsync(dto);
                                break;
                            case CategoryTopics.Deleted:
                                await handler.HandleDeletedAsync(dto);
                                break;
                            case CategoryTopics.Restored:
                                await handler.HandleRestoredAsync(dto);
                                break;
                        }
                    }

                    _consumer.Commit(result);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogWarning("Topic not available: {Error}. Waiting...", ex.Error.Reason);
                    await Task.Delay(10000, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing category event");
                    await Task.Delay(1000, stoppingToken); // backoff, then continue implicitly
                }
            }

            _consumer.Close();
        }, stoppingToken);
    }

    public override void Dispose()
    {
        _consumer.Dispose();
        base.Dispose();
    }
}