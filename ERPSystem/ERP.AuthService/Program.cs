using ERP.AuthService.Application.Interfaces;
using ERP.AuthService.Application.Services;
using ERP.AuthService.Domain;
using ERP.AuthService.Infrastructure.Configuration;
using ERP.AuthService.Infrastructure.Persistence;
using ERP.AuthService.Infrastructure.Security;
using ERP.AuthService.Middleware;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

//////////////////////////////////////////////////
// Mongo Configuration
//////////////////////////////////////////////////

builder.Services.Configure<MongoSettings>(
    builder.Configuration.GetSection("MongoSettings"));

builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<MongoSettings>>().Value;
    return new MongoClient(settings.ConnectionString);
});

//////////////////////////////////////////////////
// JWT Settings (ONLY for token generation)
//////////////////////////////////////////////////

builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("JwtSettings"));

//////////////////////////////////////////////////
// Dependency Injection
//////////////////////////////////////////////////

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAuthUserRepository, MongoAuthUserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, MongoRefreshTokenRepository>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IPasswordHasher<AuthUser>, PasswordHasher<AuthUser>>();

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();
