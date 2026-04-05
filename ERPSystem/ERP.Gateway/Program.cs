using ERP.Gateway.Properties;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
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
})
.AddJwtBearer(options =>
{
    options.MapInboundClaims = false;

    var signingKey = new SymmetricSecurityKey(
        Encoding.UTF8.GetBytes(config["JWT:Secret"]
            ?? throw new InvalidOperationException("JWT:Secret is not configured.")));
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
        RoleClaimType = "role",
        ClockSkew = TimeSpan.FromMinutes(5),
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"[Gateway] JWT validation failed: {context.Exception.Message}");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var sub = context.Principal?.FindFirst("sub")?.Value;
            Console.WriteLine($"[Gateway] Token valid for sub={sub}");
            return Task.CompletedTask;
        },
        OnChallenge = async context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
               """{"statusCode":401,"code":"AUTH_006","message":"Authentication required. Please log in."}""");
        },
        OnForbidden = async context =>
        {
            context.Response.StatusCode = 403;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                """{"statusCode":403,"code":"AUTH_007","message":"You do not have permission to access this resource."}""");
        }
    };
});

//////////////////////////////////////////////////
// Authorization
//////////////////////////////////////////////////

builder.Services.AddAuthorization(options =>
{
    // ── No FallbackPolicy — routes marked "Anonymous" in appsettings.json
    //    are intentionally public; all others have an explicit AuthorizationPolicy.
    //    FallbackPolicy would override "Anonymous" and block login/refresh/revoke.

    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.AddPolicy("JwtPolicy", p => p.RequireAuthenticatedUser()); // ← single declaration

    options.AddPolicy("AdminOnly", p =>
        p.RequireAuthenticatedUser()
         .RequireRole(Roles.SystemAdmin));

    // ── Individual privilege policies ──────────────────────────────────────
    foreach (var privilege in PrivilegeRegistry.All)
    {
        options.AddPolicy(privilege.Code, p =>
            p.RequireAuthenticatedUser()
             .RequireClaim("privilege", privilege.Code));
    }

    // ── MANAGE composite policies ──────────────────────────────────────────
    void AddManagePolicy(string manageCode, params string[] related)
    {
        options.AddPolicy(manageCode, p =>
            p.RequireAuthenticatedUser()
             .RequireAssertion(ctx =>
                 related.Any(r => ctx.User.HasClaim("privilege", r))));
    }

    AddManagePolicy(Privileges.Users.MANAGE_USERS,
        Privileges.Users.VIEW_USERS,
        Privileges.Users.CREATE_USER,
        Privileges.Users.UPDATE_USER,
        Privileges.Users.DELETE_USER,
        Privileges.Users.ACTIVATE_USER,
        Privileges.Users.DEACTIVATE_USER,
        Privileges.Users.RESTORE_USER,
        Privileges.Users.ASSIGN_ROLES);

    AddManagePolicy(Privileges.Users.MANAGE_ROLES,
        Privileges.Users.CREATE_ROLE,
        Privileges.Users.UPDATE_ROLE,
        Privileges.Users.DELETE_ROLE);

    AddManagePolicy(Privileges.Users.MANAGE_CONTROLES,
        Privileges.Users.CREATE_CONTROLE,
        Privileges.Users.UPDATE_CONTROLE,
        Privileges.Users.DELETE_CONTROLE);

    AddManagePolicy(Privileges.Clients.MANAGE_CLIENTS,
        Privileges.Clients.VIEW_CLIENTS,
        Privileges.Clients.CREATE_CLIENT,
        Privileges.Clients.UPDATE_CLIENT,
        Privileges.Clients.DELETE_CLIENT,
        Privileges.Clients.RESTORE_CLIENT,
        Privileges.Clients.CREATE_CLIENT_CATEGORIES,
        Privileges.Clients.UPDATE_CLIENT_CATEGORIES,
        Privileges.Clients.DELETE_CLIENT_CATEGORIES,
        Privileges.Clients.RESTORE_CLIENT_CATEGORIES);

    AddManagePolicy(Privileges.Articles.MANAGE_ARTICLES,
        Privileges.Articles.VIEW_ARTICLES,
        Privileges.Articles.CREATE_ARTICLE,
        Privileges.Articles.UPDATE_ARTICLE,
        Privileges.Articles.DELETE_ARTICLE,
        Privileges.Articles.RESTORE_ARTICLE,
        Privileges.Articles.CREATE_ARTICLE_CATEGORIES,
        Privileges.Articles.UPDATE_ARTICLE_CATEGORIES,
        Privileges.Articles.DELETE_ARTICLE_CATEGORIES,
        Privileges.Articles.RESTORE_ARTICLE_CATEGORIES);

    AddManagePolicy(Privileges.Invoices.MANAGE_INVOICES,
        Privileges.Invoices.VIEW_INVOICES,
        Privileges.Invoices.CREATE_INVOICE,
        Privileges.Invoices.VALIDATE_INVOICE,
        Privileges.Invoices.DELETE_INVOICE,
        Privileges.Invoices.RESTORE_INVOICE);

    AddManagePolicy(Privileges.Payments.MANAGE_PAYMENTS,
        Privileges.Payments.VIEW_PAYMENTS,
        Privileges.Payments.RECORD_PAYMENT,
        Privileges.Payments.DELETE_PAYMENT,
        Privileges.Payments.RESTORE_PAYMENT);

    AddManagePolicy(Privileges.Stock.MANAGE_STOCK,
        Privileges.Stock.VIEW_STOCK,
        Privileges.Stock.UPDATE_STOCK,
        Privileges.Stock.ADD_ENTRY);

    AddManagePolicy(Privileges.Reports.MANAGE_REPORTS,
        Privileges.Reports.VIEW_REPORTS,
        Privileges.Reports.EXPORT_REPORTS);

    options.AddPolicy(Privileges.Audit.MANAGE_AUDITLOGS, p =>
        p.RequireAuthenticatedUser()
         .RequireClaim("privilege", Privileges.Audit.MANAGE_AUDITLOGS));

    options.AddPolicy("MANAGE_CLIENTS_STOCK", p =>
    p.RequireAuthenticatedUser()
     .RequireAssertion(context =>
         context.User.Claims.Any(c =>
             c.Type == "privilege" &&
             (c.Value == Privileges.Clients.VIEW_CLIENTS ||
             c.Value == Privileges.Stock.VIEW_STOCK ||
              c.Value == Privileges.Stock.ADD_ENTRY ||
              c.Value == Privileges.Stock.UPDATE_STOCK))));
});

//////////////////////////////////////////////////
// Rate Limiting  — unchanged
//////////////////////////////////////////////////

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 200,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.AddPolicy("LoginPolicy", context =>
    {
        context.Items["RateLimitPolicyName"] = "LoginPolicy";
        return RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(5),
                QueueLimit = 0
            });
    });

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
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6,
                QueueLimit = 0
            });
    });

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
                PermitLimit = 20,
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
            "LoginPolicy" => 5 * 60,
            _ => 60
        };

        context.HttpContext.Response.Headers.RetryAfter = retrySeconds.ToString();

        string FormatWaitTime(int s) => s >= 60
            ? $"{s / 60} minute{(s / 60 > 1 ? "s" : "")}"
            : $"{s} second{(s > 1 ? "s" : "")}";

        var message = policyName switch
        {
            "LoginPolicy" => $"Too many login attempts. Please wait {FormatWaitTime(retrySeconds)} before retrying.",
            "WritePolicy" => $"Too many write operations. Please wait {FormatWaitTime(retrySeconds)} before retrying.",
            "UserPolicy" => $"Request limit reached. Please wait {FormatWaitTime(retrySeconds)} before retrying.",
            _ => $"Too many requests. Please wait {FormatWaitTime(retrySeconds)} before retrying."
        };

        await context.HttpContext.Response.WriteAsync(
            $$"""{"statusCode":429,"code":"RATE_LIMIT","message":"{{message}}","retryAfterSeconds":{{retrySeconds}}}""");
    };
});

//////////////////////////////////////////////////
// CORS  — unchanged
//////////////////////////////////////////////////

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

//////////////////////////////////////////////////
// YARP  — unchanged
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

//////////////////////////////////////////////////
// Pipeline
//////////////////////////////////////////////////

var app = builder.Build();

app.UseCors("AllowFrontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapReverseProxy();

app.Run();