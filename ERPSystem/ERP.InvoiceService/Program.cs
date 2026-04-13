using ERP.InvoiceService.Application.Interfaces;
using ERP.InvoiceService.Application.Services.LocalCache;
using ERP.InvoiceService.Application.Services.LocalCache.ArticleCache;
using ERP.InvoiceService.Application.Services.LocalCache.ClientCache;
using ERP.InvoiceService.Infrastructure.Messaging.ArticleEvents.Article;
using ERP.InvoiceService.Infrastructure.Messaging.ClientEvents.Category;
using ERP.InvoiceService.Infrastructure.Messaging.ClientEvents.Client;
using ERP.InvoiceService.Infrastructure.Persistence;
using ERP.InvoiceService.Infrastructure.Persistence.Repositories;
using ERP.InvoiceService.Infrastructure.Persistence.Repositories.LocalCache;
using ERP.InvoiceService.Infrastructure.Messaging.ArticleEvents.Category;
using InvoiceService.Application.Interfaces;
using InvoiceService.Application.Services;
using InvoiceService.Middleware;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ERP.InvoiceService.Infrastructure.Persistence.Repositories.LocalCache.ArticleCache;
using ERP.InvoiceService.Infrastructure.Persistence.Repositories.LocalCache.ClientCache;

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

// Article cache dependencies
builder.Services.AddScoped<IArticleCacheRepository, ArticleCacheRepository>();
builder.Services.AddScoped<IArticleCacheService, ArticleCacheService>();
builder.Services.AddScoped<IArticleEventHandler, ArticleEventHandler>();
builder.Services.AddHostedService<ArticleEventConsumer>();

// Article categories cache dependencies
builder.Services.AddScoped<ICategoryCacheRepository, CategoryCacheRepository>();
builder.Services.AddScoped<ICategoryCacheService, CategoryCacheService>();
builder.Services.AddScoped<ICategoryEventHandler, CategoryEventHandler>();
builder.Services.AddHostedService<CategoryEventConsumer>();

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


// =========================
// CONTROLLERS & API
// =========================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();

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