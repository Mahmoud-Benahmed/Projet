using ERP.FournisseurService.Application.Interfaces;
using ERP.FournisseurService.Application.Services;
using ERP.FournisseurService.Infrastructure.Messaging;
using ERP.FournisseurService.Infrastructure.Persistence;
using ERP.FournisseurService.Infrastructure.Persistence.Repositories;
using ERP.FournisseurService.Infrastructure.Persistence.Seeders;
using ERP.FournisseurService.Middleware;
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

builder.Services.AddDbContext<FournisseurDbContext>(options =>
    options.UseSqlServer(connectionString));

// =========================
// DEPENDENCY INJECTION
// =========================
builder.Services.AddScoped<IFournisseurRepository, FournisseurRepository>();
builder.Services.AddScoped<IFournisseurService, FournisseurService>();
builder.Services.AddSingleton<IEventPublisher, KafkaEventPublisher>();

builder.Services.AddScoped<FournisseurSeeder>();

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
builder.Services.AddDatabaseSeeders();

// =========================
// BUILD
// =========================
var app = builder.Build();

// =========================
// AUTO MIGRATE & SEED
// =========================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FournisseurDbContext>();
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();

    await db.Database.EnsureDeletedAsync();
    await db.Database.MigrateAsync();
    await seeder.SeedAsync();
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