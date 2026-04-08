

using ERP.InvoiceService.Infrastructure.Messaging;
using ERP.InvoiceService.Infrastructure.Persistence;
using InvoiceService.Application.Interfaces;
using InvoiceService.Application.Services;
using InvoiceService.Infrastructure.Seeders;
using InvoiceService.Middleware;
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


// =========================
// HTTP CLIENTS
// =========================
builder.Services.AddHttpClient<IArticleServiceHttpClient, ArticleServiceHttpClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Services:ArticleService:BaseUrl"]
            ?? throw new InvalidOperationException(
                "Services:ArticleService:BaseUrl is not configured."));
});

builder.Services.AddHttpClient<IClientServiceHttpClient, ClientServiceHttpClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Services:ClientService:BaseUrl"]
            ?? throw new InvalidOperationException(
                "Services:ClientService:BaseUrl is not configured."));
});

// =========================
// CONTROLLERS & API
// =========================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDatabaseSeeders();

var app = builder.Build();

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
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();

    await context.Database.EnsureDeletedAsync();
    await context.Database.MigrateAsync();
    await seeder.SeedAsync();

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