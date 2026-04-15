using Confluent.Kafka;
using Confluent.Kafka.Admin;
using ERP.InvoiceService.Application.Interfaces;
using ERP.InvoiceService.Application.Services.LocalCache;
using ERP.InvoiceService.Application.Services.LocalCache.ArticleCache;
using ERP.InvoiceService.Application.Services.LocalCache.ClientCache;
using ERP.InvoiceService.Infrastructure.Messaging;
using ERP.InvoiceService.Infrastructure.Messaging.Events;
using ERP.InvoiceService.Infrastructure.Messaging.Events.ArticleEvents.Article;
using ERP.InvoiceService.Infrastructure.Messaging.Events.ArticleEvents.ArticleCategory;
using ERP.InvoiceService.Infrastructure.Messaging.Events.ClientEvents.Category;
using ERP.InvoiceService.Infrastructure.Messaging.Events.ClientEvents.Client;
using ERP.InvoiceService.Infrastructure.Persistence;
using ERP.InvoiceService.Infrastructure.Persistence.Repositories;
using ERP.InvoiceService.Infrastructure.Persistence.Repositories.LocalCache;
using ERP.InvoiceService.Infrastructure.Persistence.Repositories.LocalCache.ArticleCache;
using ERP.InvoiceService.Infrastructure.Persistence.Repositories.LocalCache.ClientCache;
using InvoiceService.Application.Interfaces;
using InvoiceService.Middleware;
using InvoiceService.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

// =========================
// DATABASE
// =========================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionString 'DefaultConnection' is not configured.");

builder.Services.AddDbContext<InvoiceDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });


builder.Services.AddHttpClient<IStockServiceHttpClient, StockServiceHttpClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Services:StockService:BaseUrl"] 
        ?? throw new InvalidOperationException(
            "StockServiceUrl not configured."));
});



// =========================
// API RESPONSE NORMALIZATION
// =========================
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var message = string.Join(" | ", context.ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage));

        return new BadRequestObjectResult(new
        {
            statusCode = 400,
            code = "VALIDATION_ERROR",
            message
        });
    };
});


// =========================
// REPOSITORIES
// =========================
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();

// =========================
// SERVICES
// =========================
builder.Services.AddScoped<IInvoiceNumberGenerator, InvoiceNumberGenerator>();
builder.Services.AddScoped<IInvoicesService, InvoicesService>();

// Article cache dependencies
builder.Services.AddScoped<IArticleCacheRepository, ArticleCacheRepository>();
builder.Services.AddScoped<IArticleCacheService, ArticleCacheService>();
builder.Services.AddScoped<IArticleEventHandler, ArticleEventHandler>();
builder.Services.AddHostedService<ArticleEventConsumer>();

// Article categories cache dependencies
builder.Services.AddScoped<IArticleCategoryCacheRepository, ArticleCategoryCacheRepository>();
builder.Services.AddScoped<IArticleCategoryCacheService, ArticleCategoryCacheService>();
builder.Services.AddScoped<IArticleCategoryEventHandler, ArticleCategoryEventHandler>();
builder.Services.AddHostedService<ArticleCategoryEventConsumer>();

// Client cache dependencies
builder.Services.AddScoped<IClientCacheRepository, ClientCacheRepository>();
builder.Services.AddScoped<IClientCacheService, ClientCacheService>();
builder.Services.AddScoped<IClientEventHandler, ClientEventHandler>();
builder.Services.AddHostedService<ClientEventConsumer>();

// Client category cache dependencies
builder.Services.AddScoped<IClientCategoryCacheRepository, ClientCategoryCacheRepository>();
builder.Services.AddScoped<IClientCategoryCacheService, ClientCategoryCacheService>();
builder.Services.AddScoped<IClientCategoryEventHandler, ClientCategoryEventHandler>();
builder.Services.AddHostedService<ClientCategoryEventConsumer>();

builder.Services.AddSingleton<IEventPublisher, KafkaEventPublisher>();
builder.Services.AddScoped<IInvoicePdfGenerator, InvoicePdfGenerator>();


// =========================
// CONTROLLERS & API
// =========================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();


// =========================
// KAFKA TOPIC VERIFICATION & CREATION
// =========================
using (var scope = app.Services.CreateScope())
{
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    var bootstrapServers = configuration["Kafka:BootstrapServers"]
        ?? throw new InvalidOperationException("Kafka:BootstrapServers not configured.");

    var adminConfig = new AdminClientConfig
    {
        BootstrapServers = bootstrapServers
    };

    using var adminClient = new AdminClientBuilder(adminConfig).Build();

    var requiredTopics = new[] {
        ArticleTopics.Created, ArticleTopics.Updated,
        ArticleTopics.Deleted, ArticleTopics.Restored,

        ArticleCategoryTopics.Created, ArticleCategoryTopics.Updated,
        ArticleCategoryTopics.Deleted, ArticleCategoryTopics.Restored,

        ClientTopics.Created, ClientTopics.Updated,
        ClientTopics.Deleted, ClientTopics.Restored,

        ClientCategoryTopics.Created, ClientCategoryTopics.Updated,
        ClientCategoryTopics.Deleted, ClientCategoryTopics.Restored
    };

    var maxRetries = 30;
    var retryDelay = TimeSpan.FromSeconds(2);

    // First, try to create all topics
    var topicSpecifications = requiredTopics.Select(topic => new TopicSpecification
    {
        Name = topic,
        NumPartitions = 1,  // Adjust based on your needs
        ReplicationFactor = 1  // Adjust for your Kafka cluster
    });

    try
    {
        await adminClient.CreateTopicsAsync(topicSpecifications);
        logger.LogInformation("Successfully created all required Kafka topics");
    }
    catch (CreateTopicsException ex) when (ex.Results[0].Error.Code == ErrorCode.TopicAlreadyExists)
    {
        logger.LogInformation("Some topics already exist, continuing...");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error creating Kafka topics");
    }

    // Then verify they exist
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(10));
            var existingTopics = metadata.Topics.Select(t => t.Topic).ToHashSet();

            var missingTopics = requiredTopics.Where(t => !existingTopics.Contains(t)).ToList();

            if (!missingTopics.Any())
            {
                logger.LogInformation("All required Kafka topics exist and are ready");
                break;
            }

            logger.LogWarning("Waiting for topics to be fully created... Missing: {MissingTopics}. Attempt {Attempt}/{MaxRetries}",
                string.Join(", ", missingTopics), i + 1, maxRetries);
            await Task.Delay(retryDelay);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking Kafka topics. Attempt {Attempt}/{MaxRetries}", i + 1, maxRetries);
            await Task.Delay(retryDelay);
        }
    }
}


app.UseExceptionHandler(
    errApp => errApp.Run(async ctx =>
    {
        var feature = ctx.Features.Get<IExceptionHandlerFeature>();
        if (feature?.Error is InvoiceDomainException domainEx)
        {
            ctx.Response.StatusCode = 400;
            await ctx.Response.WriteAsJsonAsync(new { error = domainEx.Message });
        }
    })
);

// =========================
// MIGRATIONS
// =========================
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<InvoiceDbContext>();

    await context.Database.EnsureDeletedAsync();
    await context.Database.MigrateAsync();
}
app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<GlobalExceptionMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
    app.UseHsts();
}

app.UseAuthorization();
app.MapControllers();

app.Run();