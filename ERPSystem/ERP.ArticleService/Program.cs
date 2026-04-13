using ERP.ArticleService.Application.Interfaces;
using ERP.ArticleService.Application.Services;
using ERP.ArticleService.Infrastructure.Messaging;
using ERP.ArticleService.Infrastructure.Persistence;
using ERP.ArticleService.Infrastructure.Persistence.Seeders;
using ERP.ArticleService.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

// =========================
// DATABASE
// =========================

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionString 'DefaultConnection' is not configured.");

// ── Database
builder.Services.AddDbContext<ArticleDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// API responses normalization
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
            code = "VALIDATION ERROR",
            message
        });
    };
});

// =========================
// REPOSITORIES
// =========================
builder.Services.AddScoped<IArticleRepository, ArticleRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IArticleCodeRepository, ArticleCodeRepository>();

// =========================
// SERVICES
// =========================
builder.Services.AddScoped<IArticleService, ArticleService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IArticleCodeService, ArticleCodeService>();

// =========================
// MESSAGING
// =========================
builder.Services.AddSingleton<IEventPublisher, KafkaEventPublisher>();

builder.Services.AddScoped<ArticleCodeSeeder>();
builder.Services.AddScoped<CategorySeeder>();
builder.Services.AddScoped<ArticleSeeder>();

// =========================
// CONTROLLERS & API
// =========================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var context = services.GetRequiredService<ArticleDbContext>();

        logger.LogInformation("Resetting database...");
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();

        logger.LogInformation("Clearing existing data...");
        await context.ArticleCodes.IgnoreQueryFilters().ExecuteDeleteAsync();
        await context.Articles.IgnoreQueryFilters().ExecuteDeleteAsync();
        await context.Categories.IgnoreQueryFilters().ExecuteDeleteAsync();

        // ✅ RESOLVE SEEDERS FROM DI (NOT using 'new')
        logger.LogInformation("Seeding article codes...");
        var articleCodeSeeder = services.GetRequiredService<ArticleCodeSeeder>();
        await articleCodeSeeder.SeedAsync();

        logger.LogInformation("Seeding categories...");
        var categorySeeder = services.GetRequiredService<CategorySeeder>();
        await categorySeeder.SeedAsync();

        logger.LogInformation("Seeding articles...");
        var articleSeeder = services.GetRequiredService<ArticleSeeder>();
        await articleSeeder.SeedAsync();

        logger.LogInformation("✅ All seeding completed successfully!");
    }
    catch (Exception ex)
    {

        logger.LogError(ex, "❌ An error occurred while seeding the database.");
        throw;
    }
}


// =========================
// PIPELINE
// =========================
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