using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
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
        Encoding.UTF8.GetBytes(config["JWT:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret is not configured.")));
    signingKey.KeyId = "erp-key-1";

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = config["JWT:Issuer"],
        ValidAudience = config["JWT:Audience"],
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

    // ── Auth
    options.AddPolicy("ViewUsers",      p => p.RequireClaim("privilege",    "ViewUsers"));
    options.AddPolicy("CreateUser",     p => p.RequireClaim("privilege",    "CreateUser"));
    options.AddPolicy("ActivateUser",   p => p.RequireClaim("privilege",    "ActivateUser"));
    options.AddPolicy("DeactivateUser", p => p.RequireClaim("privilege",    "DeactivateUser"));
    options.AddPolicy("UpdateUser",     p => p.RequireClaim("privilege",    "UpdateUser"));
    options.AddPolicy("DeleteUser",     p => p.RequireClaim("privilege",    "DeleteUser"));
    options.AddPolicy("RestoreUser",    p => p.RequireClaim("privilege",    "RestoreUser"));

    options.AddPolicy("AssignRoles",    p => p.RequireClaim("privilege",    "AssignRoles"));

    // Audit-Log
    options.AddPolicy("ManageAuditLogs",p => p.RequireClaim("privilege",    "ManageAuditLogs"));

    // ── Clients
    options.AddPolicy("ViewClients",    p => p.RequireClaim("privilege",    "ViewClients"));
    options.AddPolicy("CreateClient",   p => p.RequireClaim("privilege",    "CreateClient"));
    options.AddPolicy("UpdateClient",   p => p.RequireClaim("privilege",    "UpdateClient"));
    options.AddPolicy("DeleteClient",   p => p.RequireClaim("privilege",    "DeleteClient"));
    options.AddPolicy("RestoreClient",  p => p.RequireClaim("privilege",    "RestoreClient"));

    // ── Articles
    options.AddPolicy("ViewArticles",   p => p.RequireClaim("privilege",    "ViewArticles"));
    options.AddPolicy("CreateArticle",  p => p.RequireClaim("privilege",    "CreateArticle"));
    options.AddPolicy("UpdateArticle",  p => p.RequireClaim("privilege",    "UpdateArticle"));
    options.AddPolicy("DeleteArticle",  p => p.RequireClaim("privilege",    "DeleteArticle"));
    options.AddPolicy("RestoreArticle", p => p.RequireClaim("privilege",    "RestoreArticle"));

    // ── Facturation
    options.AddPolicy("ViewInvoices",   p => p.RequireClaim("privilege",    "ViewInvoices"));
    options.AddPolicy("CreateInvoice",  p => p.RequireClaim("privilege",    "CreateInvoice"));
    options.AddPolicy("ValidateInvoice",p => p.RequireClaim("privilege",    "ValidateInvoice"));
    options.AddPolicy("DeleteInvoice",  p => p.RequireClaim("privilege",    "DeleteInvoice"));
    options.AddPolicy("RestoreInvoice", p => p.RequireClaim("privilege",    "RestoreInvoice"));

    // ──    Paiements
    options.AddPolicy("ViewPayments",   p => p.RequireClaim("privilege",    "ViewPayments"));
    options.AddPolicy("RecordPayment",  p => p.RequireClaim("privilege",    "RecordPayment"));
    options.AddPolicy("DeletePayment",  p => p.RequireClaim("privilege",    "DeletePayment"));
    options.AddPolicy("RestorePayment", p => p.RequireClaim("privilege",    "RestorePayment"));

    // ── Stocks
    options.AddPolicy("ViewStock",      p => p.RequireClaim("privilege",    "ViewStock"));
    options.AddPolicy("UpdateStock",    p => p.RequireClaim("privilege",    "UpdateStock"));
    options.AddPolicy("AddEntry",       p => p.RequireClaim("privilege",    "AddEntry"));

    // ── Reporting
    options.AddPolicy("ViewReports",    p => p.RequireClaim("privilege",    "ViewReports"));
    options.AddPolicy("ExportReports",  p => p.RequireClaim("privilege",    "ExportReports"));
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
    {
        context.Items["RateLimitPolicyName"] = "LoginPolicy";
        return RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,                // 5 attempts
                Window = TimeSpan.FromMinutes(5),
                QueueLimit = 0
            });
    });

    // ── AUTHENTICATED USER ACTIONS ────────────────────────
    options.AddPolicy("UserPolicy", context =>
    {
        context.Items["RateLimitPolicyName"] = "UserPolicy";
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
        context.Items["RateLimitPolicyName"] = "WritePolicy";
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

        var policyName = context.HttpContext.Items["RateLimitPolicyName"]?.ToString();
        var retrySeconds = policyName switch
        {
            "LoginPolicy" => 5 * 60,   // 5 minute window
            "WritePolicy" => 60,        // 1 minute window
            "UserPolicy" => 60,        // 1 minute window
            _ => 60         // global fallback
        };
        context.HttpContext.Response.Headers.RetryAfter = retrySeconds.ToString();
        Console.WriteLine($"retryAfter: {retrySeconds}");

        var message = policyName switch
        {
            "LoginPolicy" => $"Too many login attempts. Please wait {retrySeconds} seconds before retrying.",
            "WritePolicy" => $"Too many write operations. Please wait {retrySeconds} seconds before retrying.",
            "UserPolicy" => $"Request limit reached. Please wait {retrySeconds} seconds before retrying.",
            _ => $"Too many requests. Please wait {retrySeconds} seconds before retrying."
        };

        await context.HttpContext.Response.WriteAsync(
            $$"""{"statusCode": 429, "code": "RATE_LIMIT", "content": "{{message}}", "retryAfterSeconds": {{retrySeconds}}}""",
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