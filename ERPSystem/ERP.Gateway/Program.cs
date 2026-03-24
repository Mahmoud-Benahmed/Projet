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
              .RequireRole("SYSTEMADMIN"));
    // ── Auth
    options.AddPolicy("VIEWUSERS", p => p.RequireClaim("privilege", "VIEWUSERS"));
    options.AddPolicy("CREATEUSER", p => p.RequireClaim("privilege", "CREATEUSER"));
    options.AddPolicy("ACTIVATEUSER", p => p.RequireClaim("privilege", "ACTIVATEUSER"));
    options.AddPolicy("DEACTIVATEUSER", p => p.RequireClaim("privilege", "DEACTIVATEUSER"));
    options.AddPolicy("UPDATEUSER", p => p.RequireClaim("privilege", "UPDATEUSER"));
    options.AddPolicy("DELETEUSER", p => p.RequireClaim("privilege", "DELETEUSER"));
    options.AddPolicy("RESTOREUSER", p => p.RequireClaim("privilege", "RESTOREUSER"));

    options.AddPolicy("MANAGEUSERS", p =>
        p.RequireAssertion(context =>
            context.User.HasClaim("privilege", "VIEWUSERS") ||
            context.User.HasClaim("privilege", "CREATEUSER") ||
            context.User.HasClaim("privilege", "UPDATEUSER") ||
            context.User.HasClaim("privilege", "DELETEUSER") ||
            context.User.HasClaim("privilege", "ACTIVATEUSER") ||
            context.User.HasClaim("privilege", "DEACTIVATEUSER")
    ));

    options.AddPolicy("ASSIGNROLES", p => p.RequireClaim("privilege", "ASSIGNROLES"));


    // ── Audit
    options.AddPolicy("MANAGEAUDITLOGS", p => p.RequireClaim("privilege", "MANAGEAUDITLOGS"));


    // ── Clients
    options.AddPolicy("VIEWCLIENTS", p => p.RequireClaim("privilege", "VIEWCLIENTS"));
    options.AddPolicy("CREATECLIENT", p => p.RequireClaim("privilege", "CREATECLIENT"));
    options.AddPolicy("UPDATECLIENT", p => p.RequireClaim("privilege", "UPDATECLIENT"));
    options.AddPolicy("DELETECLIENT", p => p.RequireClaim("privilege", "DELETECLIENT"));
    options.AddPolicy("RESTORECLIENT", p => p.RequireClaim("privilege", "RESTORECLIENT"));

    options.AddPolicy("MANAGECLIENTS", p =>
        p.RequireAssertion(context =>
            context.User.HasClaim("privilege", "VIEWCLIENTS") ||
            context.User.HasClaim("privilege", "CREATECLIENT") ||
            context.User.HasClaim("privilege", "UPDATECLIENT") ||
            context.User.HasClaim("privilege", "DELETECLIENT")
    ));


    // ── Articles
    options.AddPolicy("VIEWARTICLES", p => p.RequireClaim("privilege", "VIEWARTICLES"));
    options.AddPolicy("CREATEARTICLE", p => p.RequireClaim("privilege", "CREATEARTICLE"));
    options.AddPolicy("UPDATEARTICLE", p => p.RequireClaim("privilege", "UPDATEARTICLE"));
    options.AddPolicy("DELETEARTICLE", p => p.RequireClaim("privilege", "DELETEARTICLE"));
    options.AddPolicy("RESTOREARTICLE", p => p.RequireClaim("privilege", "RESTOREARTICLE"));

    options.AddPolicy("MANAGEARTICLES", p =>
        p.RequireAssertion(context =>
            context.User.HasClaim("privilege", "VIEWARTICLES") ||
            context.User.HasClaim("privilege", "CREATEARTICLE") ||
            context.User.HasClaim("privilege", "UPDATEARTICLE") ||
            context.User.HasClaim("privilege", "DELETEARTICLE")
    ));


    // ── Invoices
    options.AddPolicy("VIEWINVOICES", p => p.RequireClaim("privilege", "VIEWINVOICES"));
    options.AddPolicy("CREATEINVOICE", p => p.RequireClaim("privilege", "CREATEINVOICE"));
    options.AddPolicy("VALIDATEINVOICE", p => p.RequireClaim("privilege", "VALIDATEINVOICE"));
    options.AddPolicy("DELETEINVOICE", p => p.RequireClaim("privilege", "DELETEINVOICE"));
    options.AddPolicy("RESTOREINVOICE", p => p.RequireClaim("privilege", "RESTOREINVOICE"));

    options.AddPolicy("MANAGEINVOICES", p =>
        p.RequireAssertion(context =>
            context.User.HasClaim("privilege", "VIEWINVOICES") ||
            context.User.HasClaim("privilege", "CREATEINVOICE") ||
            context.User.HasClaim("privilege", "VALIDATEINVOICE") ||
            context.User.HasClaim("privilege", "DELETEINVOICE")
    ));


    // ── Payments
    options.AddPolicy("VIEWPAYMENTS", p => p.RequireClaim("privilege", "VIEWPAYMENTS"));
    options.AddPolicy("RECORDPAYMENT", p => p.RequireClaim("privilege", "RECORDPAYMENT"));
    options.AddPolicy("DELETEPAYMENT", p => p.RequireClaim("privilege", "DELETEPAYMENT"));
    options.AddPolicy("RESTOREPAYMENT", p => p.RequireClaim("privilege", "RESTOREPAYMENT"));

    options.AddPolicy("MANAGEPAYMENTS", p =>
        p.RequireAssertion(context =>
            context.User.HasClaim("privilege", "VIEWPAYMENTS") ||
            context.User.HasClaim("privilege", "RECORDPAYMENT") ||
            context.User.HasClaim("privilege", "DELETEPAYMENT")
    ));


    // ── Stock
    options.AddPolicy("VIEWSTOCK", p => p.RequireClaim("privilege", "VIEWSTOCK"));
    options.AddPolicy("UPDATESTOCK", p => p.RequireClaim("privilege", "UPDATESTOCK"));
    options.AddPolicy("ADDENTRY", p => p.RequireClaim("privilege", "ADDENTRY"));

    options.AddPolicy("MANAGESTOCK", p =>
        p.RequireAssertion(context =>
            context.User.HasClaim("privilege", "VIEWSTOCK") ||
            context.User.HasClaim("privilege", "UPDATESTOCK") ||
            context.User.HasClaim("privilege", "ADDENTRY")
    ));
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
    .LoadFromConfig(config.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseCors("AllowFrontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapReverseProxy();

app.Run();