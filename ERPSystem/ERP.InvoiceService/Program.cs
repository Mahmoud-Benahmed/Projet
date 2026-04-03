using InvoiceService.Application.Interfaces;
using InvoiceService.Application.Services;
using InvoiceService.Infrastructure;
using InvoiceService.Middleware;
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
builder.Services.AddScoped<IInvoiceService, InvoiceService.Application.Services.InvoiceService>();

// =========================
// CONTROLLERS & API
// =========================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "InvoiceService API",
        Version = "v1",
        Description = "RESTful API for managing invoices"
    });
});
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// =========================
// MIGRATIONS
// =========================
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<InvoiceDbContext>();
    await context.Database.MigrateAsync();
    Console.WriteLine("✓ Database migrations applied successfully.");
}
app.UseSwagger();
app.UseSwaggerUI(c =>
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "InvoiceService v1"));

app.UseMiddleware<GlobalExceptionMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
    app.UseHsts();
}

app.UseAuthorization();
app.MapControllers();

app.Run();