using ERP.AuthService.Application.Interfaces;
using ERP.AuthService.Application.Services;
using ERP.AuthService.Domain;
using ERP.AuthService.Infrastructure.Configuration;
using ERP.AuthService.Infrastructure.Persistence;
using ERP.AuthService.Infrastructure.Security;
using ERP.AuthService.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// ── Add environment variables
builder.Configuration.AddEnvironmentVariables();

// ── Controllers & Swagger
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── Mongo GUID Serializer
BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

// ── Mongo Configuration
builder.Services.Configure<MongoSettings>(
    builder.Configuration.GetSection("Mongo"));

builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<MongoSettings>>().Value;
    return new MongoClient(settings.ConnectionString);
});

builder.Services.AddSingleton<IMongoDatabase>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<MongoSettings>>().Value;
    var client = sp.GetRequiredService<IMongoClient>();
    return client.GetDatabase(settings.DatabaseName);
});

// ── Read JWT secret from env
var jwtSecret = builder.Configuration["JWT_SECRET"] ?? "supersecretkey";

builder.Services.Configure<JwtSettings>(options =>
{
    options.Secret = jwtSecret;
});

// ── JWT Parsing (no validation, gateway already did it)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var authHeader = context.Request.Headers["Authorization"].ToString();
                if (authHeader.StartsWith("Bearer "))
                    context.Token = authHeader["Bearer ".Length..].Trim();
                return Task.CompletedTask;
            }
        };
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = false,
            SignatureValidator = (token, _) =>
                new Microsoft.IdentityModel.JsonWebTokens.JsonWebToken(token),
            RoleClaimType = "role",
            NameClaimType = "sub"
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOrHR", policy =>
        policy.RequireRole("Admin", "HR"));
});

// ── Dependency Injection
builder.Services.AddScoped<IAuthUserService, AuthUserService>();
builder.Services.AddScoped<IAuthUserRepository, MongoAuthUserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, MongoRefreshTokenRepository>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IPasswordHasher<AuthUser>, PasswordHasher<AuthUser>>();

var app = builder.Build();

// ── Initialize MongoDB indexes
using (var scope = app.Services.CreateScope())
{
    var database = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
    await MongoDbInitializer.InitializeAsync(database);
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.MapControllers();
app.Run();