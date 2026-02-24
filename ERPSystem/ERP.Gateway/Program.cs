using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

//////////////////////////////////////////////////
// JWT Authentication
//////////////////////////////////////////////////

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.MapInboundClaims = false;

    var signingKey = new SymmetricSecurityKey(
        Encoding.UTF8.GetBytes(config["Jwt:Secret"]!));
    signingKey.KeyId = "erp-key-1";

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = config["Jwt:Issuer"],
        ValidAudience = config["Jwt:Audience"],
        IssuerSigningKey = signingKey,
        RoleClaimType = "role"
    };
});

//////////////////////////////////////////////////
// Authorization
//////////////////////////////////////////////////

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("JwtPolicy", policy =>
        policy.RequireAuthenticatedUser());

    options.AddPolicy("AdminOnly", policy =>
        policy.RequireAuthenticatedUser()
              .RequireRole("SystemAdmin"));

    options.AddPolicy("AdminOrHR", policy =>
        policy.RequireAuthenticatedUser()
              .RequireRole("SystemAdmin", "HRManager"));

    options.AddPolicy("HROnly", policy =>
        policy.RequireAuthenticatedUser()
              .RequireRole("HRManager"));
});

//////////////////////////////////////////////////
// Rate Limiting
//////////////////////////////////////////////////

builder.Services.AddRateLimiter(options =>
{
    // ── GLOBAL SAFETY NET ─────────────────────────────────
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 200,              // per IP per minute
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    // ── LOGIN — strict (anti brute-force) ─────────────────
    options.AddPolicy("LoginPolicy", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,                // 5 attempts
                Window = TimeSpan.FromMinutes(5),
                QueueLimit = 0
            }));

    // ── AUTHENTICATED USER ACTIONS ────────────────────────
    options.AddPolicy("UserPolicy", context =>
    {
        var userId = context.User?.Identity?.IsAuthenticated == true
            ? context.User.FindFirst("sub")?.Value
            : context.Connection.RemoteIpAddress?.ToString();

        return RateLimitPartition.GetSlidingWindowLimiter(
            userId ?? "anonymous",
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 60,               // 60 req per minute per user
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6,          // every 10 sec
                QueueLimit = 0
            });
    });

    // ── WRITE OPERATIONS ──────────────────────────────────
    options.AddPolicy("WritePolicy", context =>
    {
        var userId = context.User?.Identity?.IsAuthenticated == true
            ? context.User.FindFirst("sub")?.Value
            : context.Connection.RemoteIpAddress?.ToString();

        return RateLimitPartition.GetFixedWindowLimiter(
            userId ?? "anonymous",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,               // 20 writes per minute
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });

    options.RejectionStatusCode = 429;

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsync(
            """{"message": "Too many requests. Please wait before retrying."}""",
            token);
    };
});

//////////////////////////////////////////////////
// CORS
//////////////////////////////////////////////////

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins("http://localhost:4200")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

//////////////////////////////////////////////////
// YARP
//////////////////////////////////////////////////

builder.Services.AddReverseProxy()
    .LoadFromConfig(config.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseCors("AllowFrontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapReverseProxy();

app.Run();