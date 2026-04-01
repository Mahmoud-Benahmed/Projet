# ERP Article Service - Product Management Microservice

## 📋 Overview

The **ERP Article Service** is a comprehensive product management microservice built with .NET 10 and SQL Server. It provides complete article and category management with support for soft deletion, unique code generation, and advanced filtering capabilities.

### Key Features

- 📦 **Article Management** - Create, update, delete, restore, and view articles
- 🏷️ **Category Management** - Hierarchical categorization with TVA (tax) rates
- 🔢 **Auto-generated Article Codes** - Unique, formatted codes (e.g., `ART-2026-000042`)
- 🧾 **Barcode Support** - Unique EAN-13 barcode validation
- 💰 **Tax Management** - Category-level TVA with article override capability
- 🔄 **Soft Delete** - Articles and categories can be restored
- 🔍 **Advanced Filtering** - Search by category, libelle, date range, and TVA ranges
- 📊 **Statistics** - Real-time counts and metrics

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────────┐
│              Article Service (Port 5227)                    │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐ │
│  │ Controllers │  │  Services   │  │   Repositories      │ │
│  │   Article   │→│  Article    │→│   Entity Framework   │ │
│  │   Category  │  │  Category   │  │   Core             │ │
│  │             │  │  CodeGen    │  │                     │ │
│  └─────────────┘  └─────────────┘  └─────────────────────┘ │
│         ↓               ↓                    ↓             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │           SQL Server Database                      │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

## 🚀 Quick Start

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (or Docker)
- [Docker](https://www.docker.com/) (optional)

### Local Development

1. **Clone the repository**
```bash
git clone <repository-url>
cd ERP.ArticleService
```

2. **Set up SQL Server**
```bash
# Using Docker
docker run -d \
  --name sqlserver \
  -e 'ACCEPT_EULA=Y' \
  -e 'SA_PASSWORD=YourStrong!Password123' \
  -p 1433:1433 \
  mcr.microsoft.com/mssql/server:2022-latest
```

3. **Configure database connection**
Create `appsettings.Development.json` (already provided) or set environment variable:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ERPArticlesDb;Integrated Security=True;TrustServerCertificate=True;"
  }
}
```

4. **Run migrations and seed data**
```bash
dotnet ef database update
```

5. **Run the service**
```bash
dotnet run
```

The service will start at `http://localhost:5227`

### Docker Deployment

```bash
# Build and run with Docker Compose
docker-compose up -d
```

The article service will be available at `http://localhost:5002`

## 📡 API Endpoints

### Article Endpoints

| Method | Endpoint | Description | Authorization |
|--------|----------|-------------|---------------|
| GET | `/articles` | Get all articles (paginated) | `MANAGE_ARTICLES` |
| GET | `/articles/deleted` | Get deleted articles | `RESTORE_ARTICLE` |
| GET | `/articles/{id}` | Get article by ID | `MANAGE_ARTICLES` |
| GET | `/articles/by-code` | Get article by code or barcode | `MANAGE_ARTICLES` |
| GET | `/articles/by-category` | Get articles by category | `MANAGE_ARTICLES` |
| GET | `/articles/by-libelle` | Search articles by libelle | `MANAGE_ARTICLES` |
| GET | `/articles/stats` | Get article statistics | `MANAGE_ARTICLES` |
| POST | `/articles` | Create new article | `CREATE_ARTICLE` |
| PUT | `/articles/{id}` | Update article | `UPDATE_ARTICLE` |
| PATCH | `/articles/restore/{id}` | Restore deleted article | `RESTORE_ARTICLE` |
| DELETE | `/articles/{id}` | Soft delete article | `DELETE_ARTICLE` |

### Category Endpoints

| Method | Endpoint | Description | Authorization |
|--------|----------|-------------|---------------|
| GET | `/articles/categories` | Get all categories | `MANAGE_ARTICLES` |
| GET | `/articles/categories/paged` | Get categories (paginated) | `MANAGE_ARTICLES` |
| GET | `/articles/categories/deleted` | Get deleted categories | `RESTORE_ARTICLE_CATEGORIES` |
| GET | `/articles/categories/{id}` | Get category by ID | `MANAGE_ARTICLES` |
| GET | `/articles/categories/by-name` | Get category by name | `MANAGE_ARTICLES` |
| GET | `/articles/categories/by-date-range` | Get categories by date range | `MANAGE_ARTICLES` |
| GET | `/articles/categories/tva/below` | Categories with TVA below threshold | `MANAGE_ARTICLES` |
| GET | `/articles/categories/tva/higher` | Categories with TVA above threshold | `MANAGE_ARTICLES` |
| GET | `/articles/categories/tva/between` | Categories with TVA in range | `MANAGE_ARTICLES` |
| GET | `/articles/categories/stats` | Get category statistics | `MANAGE_ARTICLES` |
| POST | `/articles/categories` | Create category | `CREATE_ARTICLE_CATEGORIES` |
| PUT | `/articles/categories/{id}` | Update category | `UPDATE_ARTICLE_CATEGORIES` |
| PATCH | `/articles/categories/restore/{id}` | Restore category | `RESTORE_ARTICLE_CATEGORIES` |
| DELETE | `/articles/categories/{id}` | Delete category | `DELETE_ARTICLE_CATEGORIES` |

## 📊 Data Models

### Article

```csharp
public class Article
{
    public Guid Id { get; private set; }
    public string CodeRef { get; init; }          // Auto-generated: ART-2026-000001
    public string BarCode { get; private set; }   // EAN-13 format
    public string Libelle { get; private set; }   // Description
    public decimal Prix { get; private set; }     // Price
    public decimal TVA { get; private set; }      // Tax rate
    public bool IsDeleted { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    
    // Navigation
    public Guid CategoryId { get; private set; }
    public Category Category { get; private set; }
}
```

### Category

```csharp
public class Category
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }      // Unique
    public decimal TVA { get; private set; }      // Default tax rate
    public bool IsDeleted { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
}
```

### ArticleCode Sequence

```csharp
public class ArticleCode
{
    public Guid Id { get; private set; }
    public string Prefix { get; private set; }    // "ART"
    public int LastNumber { get; private set; }   // Current counter
    public int Padding { get; private set; }      // 6 digits
    
    public string FormatCode(int year) =>
        $"{Prefix}-{year}-{LastNumber.ToString().PadLeft(Padding, '0')}";
}
```

## 🔢 Article Code Generation

The service uses a **database sequence** with row-level locking to generate unique article codes:

1. **Format**: `{Prefix}-{Year}-{SequentialNumber}`
   - Example: `ART-2026-000001`, `ART-2026-000042`

2. **Atomic Generation**:
   - Uses `UPDLOCK` and `ROWLOCK` hints for SQL Server
   - Prevents duplicate codes under concurrent requests
   - Ensures monotonic increasing sequence

3. **Persistence**: Single-row table `ArticleCodes` stores the counter

## 🧪 Testing

### Using HTTP Test File

The project includes `ERP.ArticleService.http` with test requests:

```http
### Create Article
POST {{gateway}}/api/articles
Authorization: Bearer {{adminToken}}
Content-Type: application/json

{
  "libelle": "Test Article",
  "prix": 99.99,
  "categoryId": "{{categoryId}}",
  "barCode": "1234567890123",
  "tva": 20.0
}
```

### Default Seeded Data

After seeding, the following data is available:

#### Categories
- Électronique (TVA: varies)
- Informatique (TVA: varies)
- Fournitures de bureau (TVA: varies)
- Mobilier (TVA: varies)
- Consommables (TVA: varies)
- Logiciels (TVA: varies)
- Réseaux & Télécommunications (TVA: varies)
- Outillage (TVA: varies)

#### Articles
- 24 sample articles across all categories
- Random TVA rates between 1-20%
- Unique EAN-13 barcodes

## 🗄️ Database Schema

### Tables

```sql
-- Categories
CREATE TABLE Categories (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    Name NVARCHAR(100) UNIQUE NOT NULL,
    TVA DECIMAL(5,2) NOT NULL,
    IsDeleted BIT DEFAULT 0,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL
);

-- Articles
CREATE TABLE Articles (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    CategoryId UNIQUEIDENTIFIER NOT NULL,
    CodeRef NVARCHAR(50) UNIQUE NOT NULL,
    BarCode NVARCHAR(13) UNIQUE NOT NULL,
    Libelle NVARCHAR(250) NOT NULL,
    Prix DECIMAL(18,2) NOT NULL,
    TVA DECIMAL(5,2) NOT NULL,
    IsDeleted BIT DEFAULT 0,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    FOREIGN KEY (CategoryId) REFERENCES Categories(Id)
);

-- Article Codes (Sequence)
CREATE TABLE ArticleCodes (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    Prefix NVARCHAR(10) UNIQUE NOT NULL,
    LastNumber INT DEFAULT 0,
    Padding INT DEFAULT 6
);
```

### Indexes

- **IX_Articles_CodeRef** - Unique (with filter for active articles)
- **IX_Articles_BarCode** - Unique (with filter for active articles)
- **IX_Categories_Name** - Unique
- **Global Query Filter** - `IsDeleted = false` for all queries

## 🔄 Business Rules

### Article Rules
1. **Code Generation**: Auto-generated, unique, cannot be manually set
2. **Barcode**: Unique across active articles (8-13 characters)
3. **TVA Resolution**: 
   - If article TVA provided → use article TVA
   - Else → use category TVA
4. **Price**: Must be positive
5. **Libelle**: Required, max 200 characters
6. **Soft Delete**: Articles can be restored

### Category Rules
1. **Name**: Unique, required, max 100 characters
2. **TVA**: Between 0-100%
3. **Deletion**: Categories with articles cannot be deleted
4. **Soft Delete**: Categories can be restored
5. **TVA Inheritance**: Categories provide default TVA for articles

## 🛠️ Development

### Adding a New Feature

1. **Update Domain Model**
```csharp
// In Domain/Article.cs
public string NewProperty { get; private set; }
```

2. **Update DTOs**
```csharp
// In Application/DTOs/ArticleDto.cs
public record CreateArticleRequestDto(
    // ... existing properties
    string NewProperty
);
```

3. **Update Service Logic**
```csharp
// In Application/Services/ArticleService.cs
public async Task<ArticleResponseDto> CreateAsync(CreateArticleRequestDto request)
{
    // Add business logic for NewProperty
}
```

4. **Update Repository**
```csharp
// In Infrastructure/Persistence/ArticleRepository.cs
public async Task<(List<Article> Items, int TotalCount)> GetByNewPropertyAsync(
    string newProperty, int pageNumber, int pageSize)
{
    var query = BaseQuery().Where(a => a.NewProperty == newProperty);
    return await PaginationHelper.ToPagedResultAsync(...);
}
```

5. **Add API Endpoint**
```csharp
// In API/Controllers/ArticleController.cs
[HttpGet("by-new-property")]
public async Task<ActionResult> GetByNewProperty(
    [FromQuery] string newProperty,
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 10)
{
    var result = await _articleService.GetByNewPropertyAsync(
        newProperty, pageNumber, pageSize);
    return Ok(new { result.Items, result.TotalCount });
}
```

### Adding a New Database Migration

```bash
dotnet ef migrations add AddNewProperty
dotnet ef database update
```

## 🔧 Configuration

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `ConnectionStrings__DefaultConnection` | SQL Server connection string | Required |
| `ASPNETCORE_ENVIRONMENT` | Runtime environment | `Development` |

### Docker Configuration

**docker-compose.yaml** includes:
- SQL Server 2022 container
- Article service container
- Health checks for database readiness
- Network isolation

## 🚨 Error Responses

| Status | Code | Description |
|--------|------|-------------|
| 400 | `ART_002` | Article already exists |
| 400 | `ART_003` | Article already active |
| 400 | `ART_004` | Article already inactive |
| 400 | `CAT_002` | Category already exists |
| 400 | `BAD_ARGUMENT` | Invalid argument provided |
| 404 | `ART_001` | Article not found |
| 404 | `CAT_001` | Category not found |
| 409 | `DUPLICATE_ENTRY` | Duplicate unique constraint |
| 409 | `ARTICLE_CATEGORY_DELETE_FAIL` | Category has articles |
| 500 | `DATABASE_ERROR` | Database operation failed |

## 📦 Dependencies

- **.NET 10.0** - Runtime framework
- **Microsoft.EntityFrameworkCore** (10.0.3) - ORM
- **Microsoft.EntityFrameworkCore.SqlServer** (10.0.3) - SQL Server provider
- **Microsoft.EntityFrameworkCore.Tools** (10.0.3) - Migration tools
- **Swashbuckle.AspNetCore** (10.1.4) - Swagger/OpenAPI
- **FluentValidation** (12.1.1) - Request validation

## 🐳 Docker Setup

### Build Image
```bash
docker build -t erp-article-service .
```

### Environment Variables
Create `.env` file:
```env
SQLSERVER__ERPArticlesDb_SA_PASSWORD=YourStrong!Password123
```

### Docker Compose
```yaml
services:
  article-service:
    build: .
    ports:
      - "5002:8080"
    environment:
      - ConnectionStrings__DefaultConnection=Server=sqlserver;Database=ERPArticlesDb;User Id=sa;Password=${SQLSERVER__ERPArticlesDb_SA_PASSWORD};TrustServerCertificate=true;
    depends_on:
      sqlserver:
        condition: service_healthy
    networks:
      - erp-network

  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      SA_PASSWORD: ${SQLSERVER__ERPArticlesDb_SA_PASSWORD}
      ACCEPT_EULA: "Y"
      MSSQL_PID: "Express"
    ports:
      - "1434:1433"
    healthcheck:
      test: ["CMD-SHELL", "/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P '${SQLSERVER__ERPArticlesDb_SA_PASSWORD}' -Q 'SELECT 1' -C"]
      interval: 10s
      timeout: 5s
      retries: 10
```

## 🧪 Integration Testing

### Test Users (from Auth Service)

| Role | Privileges for Articles |
|------|------------------------|
| SYSTEM_ADMIN | Full access (all operations) |
| SALES_MANAGER | View only (no create/update/delete) |
| STOCK_MANAGER | View, create, update (no delete) |
| ACCOUNTANT | No access to articles |

### Test Endpoints

```http
### Admin can create article
POST http://localhost:5002/articles
Authorization: Bearer {adminToken}
Content-Type: application/json

{
  "libelle": "Test Product",
  "prix": 99.99,
  "categoryId": "some-guid",
  "barCode": "1234567890123"
}

### Stock manager can view and update
GET http://localhost:5002/articles
Authorization: Bearer {stockToken}

### Sales manager can only view
GET http://localhost:5002/articles
Authorization: Bearer {salesToken}
```

## 🔍 Monitoring & Logging

The service logs:
- Seed operations (categories, articles, codes)
- Exceptions with full stack traces
- Database operations
- Request/response details (via middleware)

Log output example:
```
[INFO] Seeded category: 'Électronique'
[INFO] Seeded article: 'ART-2026-000001' - Écran 27 pouces Full HD
[ERROR] Failed to seed article 'Test Article'. (Duplicate barcode)
```

## 🤝 Contributing

1. Create a feature branch
2. Make your changes
3. Run migrations if needed
4. Update tests and documentation
5. Submit a pull request

---

## 🆘 Troubleshooting

### Database Connection Issues
- Verify SQL Server is running
- Check connection string credentials
- Ensure TrustServerCertificate=true for local development
- In Docker, ensure service names match container names

### Migration Errors
- Check for pending migrations: `dotnet ef migrations list`
- Force update: `dotnet ef database update`
- Reset database: `dotnet ef database drop && dotnet ef database update`

### Article Code Generation Issues
- Verify `ArticleCodes` table has one row
- Check for concurrent access conflicts
- Ensure SQL Server supports row-level locking

### Seeding Problems
- Check logs for duplicate key errors
- Verify categories exist before seeding articles
- Ensure TVA rates are within valid range

---

**Version**: 1.0.0  
**Target Framework**: .NET 10.0  
**Database**: SQL Server 2022  
**Last Updated**: April 2026