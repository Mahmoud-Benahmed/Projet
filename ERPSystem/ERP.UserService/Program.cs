using ERP.UserService.Application.Services;
using ERP.UserService.Infrastructure.Persistence;
using ERP.UserService.Middleware;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// Database
builder.Services.AddDbContext<UserDbContext>(options =>
    options.UseSqlServer(config.GetConnectionString("DefaultConnection")));

// Services
builder.Services.AddScoped<UserProfileService>();
builder.Services.AddScoped<IUserProfileRepository, UserProfileRepository>();

// Controllers & Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Middleware
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Logging Authorization header for debugging (optional)
app.Use(async (context, next) =>
{
    Console.WriteLine("Authorization header: " + context.Request.Headers["Authorization"]);
    await next();
});

app.UseMiddleware<GlobalExceptionMiddleware>();

// No JWT authentication here, the gateway already validated the token
app.UseAuthorization();

app.MapControllers();
app.Run();