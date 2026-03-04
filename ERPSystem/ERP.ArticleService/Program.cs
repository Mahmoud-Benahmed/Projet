using DotNetEnv;
using ERP.ArticleService.Application.Interfaces;
using ERP.ArticleService.Application.Services;
using ERP.ArticleService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

// =========================
// LOAD .env
// =========================
Env.Load();

var builder = WebApplication.CreateBuilder(args);

// =========================
// DATABASE
// =========================

var connectionString = builder.Configuration["ConnectionStrings:DefaultConnection"]
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

// ── Database
builder.Services.AddDbContext<ArticleDbContext>(options =>
    options.UseSqlServer(connectionString));


// =========================
// REPOSITORIES
// =========================
builder.Services.AddScoped<IArticleRepository, ArticleRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IArticleCodeRepository, ArticleCodeRepository>();

// =========================
// SERVICES
// =========================
builder.Services.AddScoped<IArticleService, ArticleService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IArticleCodeService, ArticleCodeService>();

// =========================
// CONTROLLERS & API
// =========================
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// =========================
// PIPELINE
// =========================
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();