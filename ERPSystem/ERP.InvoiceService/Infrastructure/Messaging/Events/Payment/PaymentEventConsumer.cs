using Confluent.Kafka;
using ERP.InvoiceService.Application.DTOs;
using ERP.InvoiceService.Application.Interfaces;
using ERP.InvoiceService.Infrastructure.Messaging.Events;
using System.Text.Json;

namespace ERP.InvoiceService.Infrastructure.Messaging.Events.Payment;

public sealed class PaymentEventConsumer : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PaymentEventConsumer> _logger;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public PaymentEventConsumer(
        IConfiguration configuration,
        IServiceScopeFactory scopeFactory,
        ILogger<PaymentEventConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        ConsumerConfig config = new ConsumerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"]
                ?? throw new InvalidOperationException("Kafka:BootstrapServers not configured."),
            GroupId = configuration["Kafka:ConsumerGroups:Payment"]
                ?? throw new InvalidOperationException("Kafka:ConsumerGroups:Payment not configured"),
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            AllowAutoCreateTopics = true
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
        // ✅ fix 4: no need for string.Join on a single topic
        _consumer.Subscribe(PaymentTopics.InvoicePaid);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PaymentEventConsumer started, topic: {Topic}",
            PaymentTopics.InvoicePaid);

        await Task.Run(async () =>
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    ConsumeResult<string, string> result = _consumer.Consume(stoppingToken);

                    _logger.LogDebug("Raw message received on {Topic}: {Message}",
                        result.Topic, result.Message.Value);

                    InvoicePaidEvent? dto = JsonSerializer.Deserialize<InvoicePaidEvent>(
                        result.Message.Value, _jsonOptions);

                    if (dto is null)
                    {
                        _logger.LogWarning("Null payload on {Topic}, skipping.", result.Topic);
                        _consumer.Commit(result);
                        continue;
                    }

                    _logger.LogInformation(
                        "Processing InvoicePaidEvent — PaymentId: {PaymentId}, " +
                        "InvoiceId: {InvoiceId}, PaidAmount: {PaidAmount}",
                        dto.PaymentId, dto.InvoiceId, dto.PaidAmount);

                    if (dto.PaidAmount <= 0)
                    {
                        _logger.LogError(
                            "InvoicePaidEvent has non-positive amount {Amount}. Skipping.",
                            dto.PaidAmount);
                        _consumer.Commit(result); // ✅ always commit, even on invalid messages
                        continue;
                    }

                    using IServiceScope scope = _scopeFactory.CreateScope();

                    IPaymentEventHandler handler = scope.ServiceProvider
                        .GetRequiredService<IPaymentEventHandler>();

                    switch (result.Topic)
                    {
                        case PaymentTopics.InvoicePaid:
                            await handler.HandleInvoicePaidAsync(dto);
                            break;

                        default:
                            _logger.LogWarning("Unknown topic {Topic}, skipping.", result.Topic);
                            break;
                    }

                    _consumer.Commit(result);

                    _logger.LogInformation(
                        "Successfully processed InvoicePaidEvent — PaymentId: {PaymentId}, Topic: {Topic}",
                        dto.PaymentId, result.Topic);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing payment event.");
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