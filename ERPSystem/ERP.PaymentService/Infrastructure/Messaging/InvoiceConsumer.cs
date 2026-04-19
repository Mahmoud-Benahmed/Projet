using Confluent.Kafka;
using ERP.PaymentService.Application.Interfaces;
using ERP.PaymentService.Domain.LocalCache;
using ERP.PaymentService.Infrastructure.Messaging.Events.InvoiceEvents;
using System.Text.Json;

namespace ERP.PaymentService.Infrastructure.Messaging
{
    public class InvoiceConsumer : BackgroundService
    {
        private readonly IConsumer<string, string> _consumer;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<InvoiceConsumer> _logger;

        public InvoiceConsumer(
            ILogger<InvoiceConsumer> logger,
            IConsumer<string, string> consumer,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _consumer = consumer;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _consumer.Subscribe(new[]
            {
                PaymentTopics.InvoiceCreated,
                PaymentTopics.InvoiceUpdated
            });

            _logger.LogInformation(
                "\n\nInvoiceConsumer started. Listening to topics: {Topics}\n\n",
                string.Join(", ", PaymentTopics.InvoiceCreated, PaymentTopics.InvoiceUpdated));

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

                    using var scope = _scopeFactory.CreateScope();
                    var invoiceCacheRepository = scope.ServiceProvider.GetRequiredService<IInvoiceCacheRepository>();

                    Invoice? invoice = null;

                    if (result.Topic == PaymentTopics.InvoiceCreated)
                    {
                        var ev = JsonSerializer.Deserialize<InvoiceCreatedEvent>(result.Message.Value);
                        if (ev is not null)
                        {
                            invoice = new Invoice
                            {
                                InvoiceId = ev.InvoiceId,
                                ClientId = ev.ClientId,
                                TotalTTC = ev.TotalTTC,
                                TotalPaid = ev.TotalPaid,
                                DueDate = ev.DueDate,
                                InvoiceDate = ev.InvoiceDate,
                                Status = ev.Status,
                                LateFeeApplied = ev.LateFeeApplied,
                                LateFeeAmount = ev.LateFeeAmount
                            };
                        }
                    }
                    else if (result.Topic == PaymentTopics.InvoiceUpdated)
                    {
                        var ev = JsonSerializer.Deserialize<InvoiceUpdatedEvent>(result.Message.Value);
                        if (ev is not null)
                        {
                            invoice = new Invoice
                            {
                                InvoiceId = ev.InvoiceId,
                                ClientId = ev.ClientId,
                                TotalTTC = ev.TotalTTC,
                                TotalPaid = ev.TotalPaid,
                                DueDate = ev.DueDate,
                                InvoiceDate = ev.InvoiceDate,
                                Status = ev.Status,
                                LateFeeApplied = ev.LateFeeApplied,
                                LateFeeAmount = ev.LateFeeAmount
                            };
                        }
                    }

                    if (invoice is not null)
                    {
                        await invoiceCacheRepository.UpsertAsync(invoice);

                        _logger.LogInformation(
                            "\n\nInvoice cache upserted for InvoiceId={InvoiceId}\n\n",
                            invoice.InvoiceId);
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("\n\nInvoiceConsumer shutting down\n\n");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "\n\nError processing invoice event\n\n");
                    await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
                }
            }

            _consumer.Close();
        }
    }
}
