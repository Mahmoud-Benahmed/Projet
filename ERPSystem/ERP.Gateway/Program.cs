using ERP.Gateway.Properties;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Yarp.ReverseProxy.Transforms;

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
              .RequireRole(Roles.SystemAdmin));

    // ── Individual privilege policies ──────────────────────────────
    foreach (var privilege in PrivilegeRegistry.All)
    {
        options.AddPolicy(privilege.Code, p =>
            p.RequireAuthenticatedUser()
             .RequireClaim("privilege", privilege.Code));
    }

    // ── MANAGE policies ────────────────────────────────────────────
    void AddManagePolicy(string manageCode, params string[] relatedPrivileges)
    {
        options.AddPolicy(manageCode, p =>
            p.RequireAuthenticatedUser()
             .RequireAssertion(context =>
                 relatedPrivileges.Any(rp => context.User.HasClaim("privilege", rp))
             ));
    }

    AddManagePolicy(Privileges.Users.MANAGE_USERS,
        Privileges.Users.VIEW_USERS,
        Privileges.Users.CREATE_USER,
        Privileges.Users.UPDATE_USER,
        Privileges.Users.DELETE_USER,
        Privileges.Users.ACTIVATE_USER,
        Privileges.Users.DEACTIVATE_USER,
        Privileges.Users.RESTORE_USER,
        Privileges.Users.ASSIGN_ROLES
    );

    AddManagePolicy(Privileges.Users.MANAGE_ROLES,
        Privileges.Users.CREATE_ROLE,
        Privileges.Users.UPDATE_ROLE,
        Privileges.Users.DELETE_ROLE
    );

    AddManagePolicy(Privileges.Users.MANAGE_CONTROLES,
        Privileges.Users.CREATE_CONTROLE,
        Privileges.Users.UPDATE_CONTROLE,
        Privileges.Users.DELETE_CONTROLE
    );

    AddManagePolicy(Privileges.Clients.MANAGE_CLIENTS,
        Privileges.Clients.VIEW_CLIENTS,
        Privileges.Clients.CREATE_CLIENT,
        Privileges.Clients.UPDATE_CLIENT,
        Privileges.Clients.DELETE_CLIENT,
        Privileges.Clients.RESTORE_CLIENT,
        Privileges.Clients.CREATE_CLIENT_CATEGORIES,
        Privileges.Clients.UPDATE_CLIENT_CATEGORIES, 
        Privileges.Clients.DELETE_CLIENT_CATEGORIES,
        Privileges.Clients.RESTORE_CLIENT_CATEGORIES
    );

    AddManagePolicy(Privileges.Articles.MANAGE_ARTICLES,
        Privileges.Articles.VIEW_ARTICLES,
        Privileges.Articles.CREATE_ARTICLE,
        Privileges.Articles.UPDATE_ARTICLE,
        Privileges.Articles.DELETE_ARTICLE,
        Privileges.Articles.RESTORE_ARTICLE,
        Privileges.Articles.CREATE_ARTICLE_CATEGORIES,
        Privileges.Articles.UPDATE_ARTICLE_CATEGORIES,
        Privileges.Articles.DELETE_ARTICLE_CATEGORIES,
        Privileges.Articles.RESTORE_ARTICLE_CATEGORIES
    );

    AddManagePolicy(Privileges.Invoices.MANAGE_INVOICES,
        Privileges.Invoices.VIEW_INVOICES,
        Privileges.Invoices.CREATE_INVOICE,
        Privileges.Invoices.VALIDATE_INVOICE,
        Privileges.Invoices.DELETE_INVOICE,
        Privileges.Invoices.RESTORE_INVOICE
    );

    AddManagePolicy(Privileges.Payments.MANAGE_PAYMENTS,
        Privileges.Payments.VIEW_PAYMENTS,
        Privileges.Payments.RECORD_PAYMENT,
        Privileges.Payments.DELETE_PAYMENT,
        Privileges.Payments.RESTORE_PAYMENT
    );

    AddManagePolicy(Privileges.Stock.MANAGE_STOCK,
        Privileges.Stock.VIEW_STOCK,
        Privileges.Stock.UPDATE_STOCK,
        Privileges.Stock.ADD_ENTRY
    );

    AddManagePolicy(Privileges.Reports.MANAGE_REPORTS,
        Privileges.Reports.VIEW_REPORTS,
        Privileges.Reports.EXPORT_REPORTS
    );


    options.AddPolicy(Privileges.Audit.MANAGE_AUDITLOGS,
        p => p.RequireAuthenticatedUser()
              .RequireClaim("privilege", Privileges.Audit.MANAGE_AUDITLOGS));
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

        string FormatWaitTime(int seconds) => seconds >= 60 ? $"{seconds / 60} minute{(seconds / 60 > 1 ? "s" : "")}"
                                                            : $"{seconds} second{(seconds > 1 ? "s" : "")}";

        var message = policyName switch
        {
            "LoginPolicy" => $"Too many login attempts. Please wait {FormatWaitTime(retrySeconds)} before retrying.",
            "WritePolicy" => $"Too many write operations. Please wait {FormatWaitTime(retrySeconds)} before retrying.",
            "UserPolicy" => $"Request limit reached. Please wait {FormatWaitTime(retrySeconds)} before retrying.",
            _ => $"Too many requests. Please wait {FormatWaitTime(retrySeconds)} before retrying."
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
    .LoadFromConfig(config.GetSection("ReverseProxy"))
    .AddTransforms(context =>
    {
        context.AddRequestTransform(async transformContext =>
        {
            var user = transformContext.HttpContext.User;

            var sub = user.FindFirstValue("sub");
            if (!string.IsNullOrEmpty(sub))
                transformContext.ProxyRequest.Headers
                    .TryAddWithoutValidation("X-User-Id", sub);

            var role = user.FindFirstValue("role");
            if (!string.IsNullOrEmpty(role))
                transformContext.ProxyRequest.Headers
                    .TryAddWithoutValidation("X-User-Role", role);
        });
    });

var app = builder.Build();

app.UseCors("AllowFrontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapReverseProxy();

app.Run();