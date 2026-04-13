
using ERP.StockService.Application.Interfaces;
using ERP.StockService.Application.Services;
using ERP.StockService.Application.Services.LocalCache;
using ERP.StockService.Application.Services.LocalCache.ArticleCache;
using ERP.StockService.Application.Services.LocalCache.ClientCache;
using ERP.StockService.Application.Services.LocalCache.Fournisseur;
using ERP.StockService.Infrastructure.Messaging;
using ERP.StockService.Infrastructure.Messaging.ArticleEvents.Article;
using ERP.StockService.Infrastructure.Messaging.ArticleEvents.Category;
using ERP.StockService.Infrastructure.Messaging.ClientEvents.Category;
using ERP.StockService.Infrastructure.Messaging.ClientEvents.Client;
using ERP.StockService.Infrastructure.Messaging.FournisseurEvents;
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
builder.Services.AddScoped<ICategoryCacheRepository, CategoryCacheRepository>();
builder.Services.AddScoped<ICategoryCacheService, CategoryCacheService>();
builder.Services.AddScoped<ICategoryEventHandler, CategoryEventHandler>();
builder.Services.AddHostedService<CategoryEventConsumer>();

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