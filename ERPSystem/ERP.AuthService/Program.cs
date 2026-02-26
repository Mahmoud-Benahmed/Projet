using ERP.AuthService.Application.Interfaces;
using ERP.AuthService.Application.Interfaces.Repositories;
using ERP.AuthService.Application.Interfaces.Services;
using ERP.AuthService.Application.Services;
using ERP.AuthService.Domain;
using ERP.AuthService.Infrastructure.Configuration;
using ERP.AuthService.Infrastructure.Messaging;
using ERP.AuthService.Infrastructure.Persistence;
using ERP.AuthService.Infrastructure.Persistence.Repositories;
using ERP.AuthService.Infrastructure.Security;
using ERP.AuthService.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using System.Text;

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
    builder.Configuration.GetSection("MongoSettings"));


builder.Services.AddSingleton<MongoDbContext>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<MongoSettings>>().Value;
    return new MongoDbContext(settings.ConnectionString, settings.DatabaseName);
});

// ── Read JWT secret from env
// ── JWT Settings
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("JwtSettings"));

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

        var key = Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Secret"] ?? throw new Exception("JwtSettings:Secret not found")) ;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"] ?? throw new Exception("JwtSettings:Secret not found"),

            ValidateAudience = true,
            ValidAudience = builder.Configuration["JwtSettings:Audience"] ?? throw new Exception("JwtSettings:Secret not found"),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5),

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),

            RoleClaimType = "role",
            NameClaimType = "sub"
        };
    });

// ── Dependency Injection
builder.Services.AddScoped<IAuthUserRepository, AuthUserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IControleRepository, ControleRepository>();
builder.Services.AddScoped<IPrivilegeRepository, PrivilegeRepository>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IAuthUserService, AuthUserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IControleService, ControleService>();
builder.Services.AddScoped<IPrivilegeService, PrivilegeService>();
builder.Services.AddScoped<IPasswordHasher<AuthUser>, PasswordHasher<AuthUser>>();
builder.Services.AddSingleton<IEventPublisher, KafkaEventPublisher>();

var app = builder.Build();
// ── Initialize MongoDB indexes
var mongoContext = app.Services.GetRequiredService<MongoDbContext>();
await MongoDbInitializer.InitializeAsync(mongoContext);

// ── Seed data
using (var scope = app.Services.CreateScope())
{
    var userRepository = scope.ServiceProvider.GetRequiredService<IAuthUserRepository>();
    var privilegeRepository = scope.ServiceProvider.GetRequiredService<IPrivilegeRepository>();
    var controleRepository = scope.ServiceProvider.GetRequiredService<IControleRepository>();
    var roleRepository = scope.ServiceProvider.GetRequiredService<IRoleRepository>();
    var refreshTokenRepository= scope.ServiceProvider.GetRequiredService<IRefreshTokenRepository>();
    var authUserService = scope.ServiceProvider.GetRequiredService<IAuthUserService>();


    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    bool resetDb = configuration.GetValue<bool>("RunSeeders:ResetDatabase");
    if (resetDb)
    {
        await privilegeRepository.DeleteAllAsync();
        await controleRepository.DeleteAllAsync();
        await roleRepository.DeleteAllAsync();
        await refreshTokenRepository.DeleteAllAsync();
        await userRepository.DeleteAllAsync();

    }

    var services = scope.ServiceProvider;
    await AuthServiceSeeder.SeedAsync(
        services.GetRequiredService<IAuthUserRepository>(),
        services.GetRequiredService<IRoleRepository>(),
        services.GetRequiredService<IControleRepository>(),
        services.GetRequiredService<IPrivilegeRepository>(),
        services.GetRequiredService<IAuthUserService>(),
        services.GetRequiredService<IConfiguration>(),
        services.GetRequiredService<IEventPublisher>()  // ← add this
    );
    
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.MapControllers();
app.Run();