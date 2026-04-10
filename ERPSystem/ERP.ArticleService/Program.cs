using ERP.ArticleService.Application.Interfaces;
using ERP.ArticleService.Application.Services;
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
// CONTROLLERS & API
// =========================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ArticleDbContext>();
    await context.Database.EnsureDeletedAsync();
    await context.Database.MigrateAsync();

    await context.ArticleCodes.IgnoreQueryFilters().ExecuteDeleteAsync();
    await context.Articles.IgnoreQueryFilters().ExecuteDeleteAsync();
    await context.Categories.IgnoreQueryFilters().ExecuteDeleteAsync();

    var articleCodeSeeder = new ArticleCodeSeeder(
        context,
        scope.ServiceProvider.GetRequiredService<ILogger<ArticleCodeSeeder>>());
    await articleCodeSeeder.SeedAsync();

    var categorySeeder = new CategorySeeder(
        scope.ServiceProvider.GetRequiredService<ICategoryService>(),
        scope.ServiceProvider.GetRequiredService<ILogger<CategorySeeder>>());
    await categorySeeder.SeedAsync();

    var articleSeeder = new ArticleSeeder(
        scope.ServiceProvider.GetRequiredService<IArticleService>(),
        scope.ServiceProvider.GetRequiredService<ICategoryService>(),
        scope.ServiceProvider.GetRequiredService<ILogger<ArticleSeeder>>());
    await articleSeeder.SeedAsync();
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