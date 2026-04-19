using Confluent.Kafka;
using ERP.PaymentService.Application.Interfaces;
using ERP.PaymentService.Application.Services;
using ERP.PaymentService.Infrastructure.Http;
using ERP.PaymentService.Infrastructure.Messaging;
using ERP.PaymentService.Infrastructure.Persistence;
using ERP.PaymentService.Infrastructure.Persistence.LocalCache.ClientCache;
using ERP.PaymentService.Infrastructure.Persistence.LocalCache.InvoiceCache;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Repositories ──────────────────────────────────────────────────────────────
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<ILateFeePolicyRepository, LateFeePolicyRepository>();
builder.Services.AddScoped<IInvoiceCacheRepository, InvoiceCacheRepository>();
builder.Services.AddScoped<IClientCacheRepository, ClientCacheRepository>();

// ── Application Services ──────────────────────────────────────────────────────
builder.Services.AddScoped<IPaymentsService, PaymentsService>();
builder.Services.AddScoped<ILateFeeePoliciesService, LateFeeePoliciesService>();

// ── Kafka Producer ────────────────────────────────────────────────────────────
var kafkaBootstrap = builder.Configuration["Kafka:BootstrapServers"]
    ?? throw new InvalidOperationException("Kafka:BootstrapServers is not configured.");

builder.Services.AddSingleton<IProducer<string, string>>(_ =>
{
    var config = new ProducerConfig { BootstrapServers = kafkaBootstrap };
    return new ProducerBuilder<string, string>(config).Build();
});

builder.Services.AddSingleton<IKafkaEventPublisher, KafkaEventPublisher>();

// ── Kafka Consumers (Background Services) ─────────────────────────────────────
builder.Services.AddSingleton<IConsumer<string, string>>(_ =>
{
    var consumerGroup = builder.Configuration["Kafka:ConsumerGroups:PaymentDecision"]
        ?? "payment-service-decision";

    var config = new ConsumerConfig
    {
        BootstrapServers = kafkaBootstrap,
        GroupId = consumerGroup,
        AutoOffsetReset = AutoOffsetReset.Earliest,
        EnableAutoCommit = true
    };
    return new ConsumerBuilder<string, string>(config).Build();
});

builder.Services.AddHostedService<InvoiceConsumer>();
builder.Services.AddHostedService<ClientConsumer>();

// ── HTTP Client → InvoiceService ──────────────────────────────────────────────
builder.Services.AddHttpClient<IInvoiceServiceHttpClient, InvoiceServiceHttpClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Services:InvoiceService:BaseUrl"]
        ?? throw new InvalidOperationException("Services:InvoiceService:BaseUrl is not configured."));
});

// ── Controllers + Swagger ─────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ERP Payment Service",
        Version = "v1",
        Description = "Manages payments, late fee policies, and invoice payment tracking."
    });
    c.EnableAnnotations();

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

// ── Database Init ─────────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    db.Database.EnsureDeleted();
    db.Database.EnsureCreated();
}

// ── Swagger ───────────────────────────────────────────────────────────────────
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ERP Payment Service v1");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "ERP Payment Service";
    c.DefaultModelsExpandDepth(-1);
});

app.MapControllers();
app.Run();