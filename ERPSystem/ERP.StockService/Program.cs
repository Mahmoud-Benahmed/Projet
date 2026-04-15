
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using ERP.StockService.Application.Interfaces;
using ERP.StockService.Application.Services;
using ERP.StockService.Application.Services.LocalCache;
using ERP.StockService.Application.Services.LocalCache.ArticleCache;
using ERP.StockService.Application.Services.LocalCache.ClientCache;
using ERP.StockService.Application.Services.LocalCache.Fournisseur;
using ERP.StockService.Application.Services.LocalCache.InvoiceCache;
using ERP.StockService.Infrastructure.Messaging;
using ERP.StockService.Infrastructure.Messaging.Events.ArticleEvents.Article;
using ERP.StockService.Infrastructure.Messaging.Events.ArticleEvents.Category;
using ERP.StockService.Infrastructure.Messaging.Events.ClientEvents.Category;
using ERP.StockService.Infrastructure.Messaging.Events.ClientEvents.Client;
using ERP.StockService.Infrastructure.Messaging.Events.FournisseurEvents;
using ERP.StockService.Infrastructure.Messaging.Events.InvoiceEvents;
using ERP.StockService.Infrastructure.Persistence;
using ERP.StockService.Infrastructure.Persistence.Repositories;
using ERP.StockService.Infrastructure.Persistence.Repositories.LocalCache;
using ERP.StockService.Infrastructure.Persistence.Repositories.LocalCache.ArticleCache;
using ERP.StockService.Infrastructure.Persistence.Repositories.LocalCache.ClientCache;
using ERP.StockService.Infrastructure.Persistence.Seeders;
using ERP.StockService.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

// =========================
// DATABASE
// =========================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "ConnectionString 'DefaultConnection' is not configured.");

builder.Services.AddDbContext<StockDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

// =========================
// DEPENDENCY INJECTION
// =========================

// Add services to the container.
builder.Services.AddScoped<IBonEntreRepository, BonEntreRepository>();
builder.Services.AddScoped<IBonSortieRepository, BonSortieRepository>();
builder.Services.AddScoped<IBonRetourRepository, BonRetourRepository>();

builder.Services.AddScoped<IBonNumeroRepository, BonNumeroRepository>();
builder.Services.AddScoped<IBonEntreService, BonEntreService>();
builder.Services.AddScoped<IBonSortieService, BonSortieService>();
builder.Services.AddScoped<IBonRetourService, BonRetourService>();
builder.Services.AddScoped<IJournalStockRepository, JournalStockRepository>();

// Article cache dependencies
builder.Services.AddScoped<IArticleCacheRepository, ArticleCacheRepository>();
builder.Services.AddScoped<IArticleCacheService, ArticleCacheService>();
builder.Services.AddScoped<IArticleEventHandler, ArticleEventHandler>();
builder.Services.AddHostedService<ArticleEventConsumer>();

// Category cache dependencies
builder.Services.AddScoped<IArticleCategoryCacheRepository, ArticleCategoryCacheRepository>();
builder.Services.AddScoped<IArticleCategoryCacheService, ArticleCategoryCacheService>();
builder.Services.AddScoped<IArticleCategoryEventHandler, ArticleCategoryEventHandler>();
builder.Services.AddHostedService<ArticleCategoryEventConsumer>();

// Client cache dependencies
builder.Services.AddScoped<IClientCacheRepository, ClientCacheRepository>();
builder.Services.AddScoped<IClientCacheService, ClientCacheService>();
builder.Services.AddScoped<IClientEventHandler, ClientEventHandler>();
builder.Services.AddHostedService<ClientEventConsumer>();

builder.Services.AddScoped<IClientCategoryCacheRepository, ClientCategoryCacheRepository>();
builder.Services.AddScoped<IClientCategoryCacheService, ClientCategoryCacheService>();
builder.Services.AddScoped<IClientCategoryEventHandler, ClientCategoryEventHandler>();
builder.Services.AddHostedService<ClientCategoryEventConsumer>();

builder.Services.AddScoped<IFournisseurCacheRepository, FournisseurCacheRepository>();
builder.Services.AddScoped<IFournisseurCacheService, FournisseurCacheService>();
builder.Services.AddScoped<IFournisseurEventHandler, FournisseurEventHandler>();
builder.Services.AddHostedService<FournisseurEventConsumer>();

builder.Services.AddScoped<IInvoiceCacheService, InvoiceCacheService>();
builder.Services.AddScoped<IInvoiceEventHandler, InvoiceEventHandler>();
builder.Services.AddHostedService<InvoiceEventConsumer>();

builder.Services.AddScoped<IInvoiceBonSortieMappingRepository, InvoiceBonSortieMappingRepository>();


// =========================
// SEEDERS
// =========================
builder.Services.AddStockSeeders(); // Add this line!

// =========================
// CONTROLLERS & API
// =========================
builder.Services
    .AddControllers(options =>
        options.SuppressAsyncSuffixInActionNames = false)
    .ConfigureApiBehaviorOptions(options =>
    {
        // Unified validation error response — Data Annotations return this shape
        options.InvalidModelStateResponseFactory = context =>
        {
            var message = string.Join(" | ", context.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));

            return new BadRequestObjectResult(new
            {
                statusCode = 400,
                code = "VALIDATION ERROR",
                message
            });
        };
    });

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
        ClientCategoryTopics.Deleted, ClientCategoryTopics.Restored,

        InvoiceTopics.Created, InvoiceTopics.Cancelled,
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

// =========================
// MIGRATIONS & SEEDING
// =========================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<StockDbContext>();
    await db.Database.EnsureDeletedAsync();
    await db.Database.MigrateAsync(); // Make this async

    // Use the seeded instance instead of static method
    var seeder = scope.ServiceProvider.GetRequiredService<StockDbSeeder>();
    await seeder.SeedAsync();
}

app.UseSwagger();
app.UseSwaggerUI();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
    app.UseHsts();
}

app.UseMiddleware<GlobalExceptionMiddleware>();
app.MapControllers();

app.Run();