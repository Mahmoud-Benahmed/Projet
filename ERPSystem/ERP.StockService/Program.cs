using ERP.StockService.Application.Interfaces;
using ERP.StockService.Application.Services;
using ERP.StockService.Infrastructure.Persistence;
using ERP.StockService.Infrastructure.Persistence.Messaging;
using ERP.StockService.Infrastructure.Persistence.Repositories;
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

// =========================
// HTTP CLIENTS
// =========================
builder.Services.AddHttpClient<IArticleService, ArticleServiceClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Services:ArticleService:BaseUrl"]
            ?? throw new InvalidOperationException(
                "Services:ArticleService:BaseUrl is not configured."));
});

builder.Services.AddHttpClient<IClientService, ClientServiceHttpClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Services:ClientService:BaseUrl"]
            ?? throw new InvalidOperationException(
                "Services:ClientService:BaseUrl is not configured."));
});

// =========================
// DEPENDENCY INJECTION
// =========================

// Add services to the container.
builder.Services.AddScoped<IBonEntreRepository, BonEntreRepository>();
builder.Services.AddScoped<IBonSortieRepository, BonSortieRepository>();
builder.Services.AddScoped<IBonRetourRepository, BonRetourRepository>();
builder.Services.AddScoped<IFournisseurRepository, FournisseurRepository>();

builder.Services.AddScoped<IBonNumeroRepository, BonNumeroRepository>();
builder.Services.AddScoped<IBonEntreService,  BonEntreService>();
builder.Services.AddScoped<IBonSortieService, BonSortieService>();
builder.Services.AddScoped<IBonRetourService, BonRetourService>();
builder.Services.AddScoped<IFournisseurService, FournisseurService>();

// =========================
// CONTROLLERS & API
// =========================
builder.Services
    .AddControllers(options =>
        options.SuppressAsyncSuffixInActionNames = false)
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles; // ← add this
    }).ConfigureApiBehaviorOptions(options =>
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

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<StockDbContext>();
    db.Database.Migrate();
    await StockDbSeeder.SeedAsync(db);
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