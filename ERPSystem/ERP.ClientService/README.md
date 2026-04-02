# ERP Client Service

A microservice-based client management system for Enterprise Resource Planning (ERP), providing comprehensive client and category management capabilities with business rules enforcement, soft delete, and advanced filtering.

## Overview

The ERP Client Service is a dedicated microservice responsible for managing client entities and their categories within an ERP ecosystem. It handles client registration, category assignments, credit limit management, return window (délai retour) calculations, and order validation rules. The service implements domain-driven design principles with clear separation of concerns and enforces business rules at the domain level.

### Key Features
- **Client Management**: Create, update, block/unblock, and soft-delete clients
- **Category Management**: Create, update, activate/deactivate, and soft-delete categories with business rules
- **Category Assignment**: Assign categories to clients with audit tracking (who assigned and when)
- **Credit Limit Management**: Set client-specific credit limits with optional category-based multipliers
- **Return Window Management**: Configure return windows at client or category level with effective value calculation
- **Order Validation**: Determine if a client can place orders based on credit limits and block status
- **Pagination & Filtering**: Filter clients/categories by name, category, or deletion status
- **Statistics**: Aggregate statistics for clients and categories

## Architecture

The project follows a **Layered Architecture** with **Domain-Driven Design (DDD)** principles, incorporating:

### Architectural Layers

| Layer | Responsibility | Key Components |
|-------|----------------|----------------|
| **Presentation** | HTTP request handling | Controllers, Routes |
| **Application** | Business logic orchestration | Services, DTOs, Interfaces |
| **Domain** | Core business rules | Entities, Value Objects |
| **Infrastructure** | Data persistence, external services | Repositories, DbContext, Seeders |

### Patterns Used
- **Repository Pattern**: Abstracts data access logic
- **Dependency Injection**: All dependencies are injected via constructor
- **DTO Pattern**: Separates domain models from API contracts
- **Global Exception Handling**: Centralized error handling middleware
- **Soft Delete Pattern**: Records are marked as deleted rather than removed
- **Query Filtering**: Global filters for soft-deleted records

### Data Flow

```
HTTP Request → Controller → Service → Repository → Database
                    ↓           ↓
                 DTOs      Domain Models
                    ↓           ↓
HTTP Response ← DTOs ← Service ← Repository
```

## Project Structure

```
ERP.ClientService/
├── Controllers/
│   ├── ClientController.cs      # Client-related endpoints
│   └── CategoryController.cs    # Category-related endpoints
└── Routes/
│       └── ApiRoutes.cs             # Centralized route definitions
├── Application/
│   ├── DTOs/                        # Data transfer objects
│   │   ├── ClientDto.cs
│   │   ├── CategoryDto.cs
│   │   ├── ErrorResponse.cs
│   │   └── PagedResultDto.cs
│   ├── Exceptions/                  # Custom exceptions
│   │   ├── ClientException.cs
│   │   └── CategoryException.cs
│   ├── Interfaces/                  # Service interfaces
│   │   ├── IClientService.cs
│   │   ├── IClientRepository.cs
│   │   ├── ICategoryService.cs
│   │   └── ICategoryRepository.cs
│   ├── Services/                    # Business logic implementation
│   │   ├── ClientService.cs
│   │   └── CategoryService.cs
│   └── Validators/                  # FluentValidation validators
│       └── DtosValidator.cs
├── Domain/                          # Domain entities
│   ├── Client.cs
│   ├── Category.cs
│   └── ClientCategory.cs           # Many-to-many join entity
├── Infrastructure/
│   ├── Persistence/
│   │   ├── ClientDbContext.cs      # EF Core DbContext
│   │   ├── Repositories/
│   │   │   ├── ClientRepository.cs
│   │   │   └── CategoryRepository.cs
│   │   └── Seeders/                 # Database seeders
│   │       ├── DatabaseSeeder.cs
│   │       ├── CategorySeeder.cs
│   │       └── ClientSeeder.cs
│   └── ...
├── Middleware/
│   └── GlobalExceptionMiddleware.cs # Centralized exception handling
├── Program.cs                       # Application entry point
├── Dockerfile                       # Docker configuration
├── docker-compose.yaml              # Multi-container orchestration
└── appsettings.json                 # Application configuration
```

## Key Components

### Controllers

#### ClientController
Handles all client-related operations with endpoints for CRUD operations, status management, and business rule enforcement.

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/clients` | Get all clients (paginated) |
| GET | `/clients/{id}` | Get client by ID |
| GET | `/clients/deleted` | Get soft-deleted clients |
| GET | `/clients/by-category` | Get clients by category |
| GET | `/clients/by-name` | Search clients by name |
| GET | `/clients/stats` | Get client statistics |
| POST | `/clients` | Create new client |
| PUT | `/clients/{id}` | Update client |
| DELETE | `/clients/{id}` | Soft delete client |
| PATCH | `/clients/restore/{id}` | Restore soft-deleted client |
| PATCH | `/clients/block/{id}` | Block client |
| PATCH | `/clients/unblock/{id}` | Unblock client |
| PUT | `/clients/{id}/credit-limit` | Set credit limit |
| DELETE | `/clients/{id}/credit-limit` | Remove credit limit |
| PUT | `/clients/{id}/return-window` | Set return window |
| DELETE | `/clients/{id}/return-window` | Clear return window |
| GET | `/clients/{id}/return-window/effective` | Get effective return window |
| GET | `/clients/{id}/can-place-order` | Check if client can place order |
| POST | `/clients/{id}/categories` | Assign category to client |
| DELETE | `/clients/{id}/categories/{categoryId}` | Remove category from client |

#### CategoryController
Manages categories with activation/deactivation capabilities and business rule validation.

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/clients/categories` | Get all categories |
| GET | `/clients/categories/paged` | Get categories (paginated) |
| GET | `/clients/categories/{id}` | Get category by ID |
| GET | `/clients/categories/deleted` | Get soft-deleted categories |
| GET | `/clients/categories/by-name` | Search categories by name |
| GET | `/clients/categories/stats` | Get category statistics |
| POST | `/clients/categories` | Create new category |
| PUT | `/clients/categories/{id}` | Update category |
| DELETE | `/clients/categories/{id}` | Soft delete category |
| PATCH | `/clients/categories/restore/{id}` | Restore category |
| PATCH | `/clients/categories/activate/{id}` | Activate category |
| PATCH | `/clients/categories/deactivate/{id}` | Deactivate category |

### Services

#### ClientService
Implements `IClientService` and orchestrates client business logic:

- **Creation/Update**: Validates unique email and business rules
- **Credit Management**: Sets/removes credit limits with validation
- **Return Window Management**: Configures client-specific return windows
- **Category Assignment**: Manages category relationships with audit tracking
- **Order Validation**: Determines if client can place orders based on:
  - Block status
  - Credit limit (client or category-multiplied)
  - Current balance vs. order amount
- **Effective Value Calculation**: Computes effective return window by checking client value first, then falling back to the maximum from assigned active categories

#### CategoryService
Implements `ICategoryService` and handles category operations:

- **Creation/Update**: Validates unique code and business rules
- **Activation/Deactivation**: Controls category availability for assignment
- **Delete Validation**: Prevents deletion if category is assigned to any client
- **Statistics**: Provides aggregate category metrics

### Repositories

#### ClientRepository
- Implements `IClientRepository` using Entity Framework Core
- Provides methods for filtered queries, pagination, and statistics
- Uses `IgnoreQueryFilters()` for accessing soft-deleted records
- Includes navigation properties for ClientCategories and Categories

#### CategoryRepository
- Implements `ICategoryRepository` with similar patterns to ClientRepository
- Handles paginated queries with name filtering
- Provides statistics aggregation
- Manages ClientCategories relationship for client count calculation

### Domain Models

#### Client
The central aggregate root with comprehensive business rules:

```csharp
public class Client
{
    // Properties
    Guid Id, string Name, string Email, string Address, string? Phone,
    string? TaxNumber, decimal? CreditLimit, int? DelaiRetour,
    bool IsBlocked, bool IsDeleted, DateTime CreatedAt, DateTime? UpdatedAt
    
    // Collections
    List<ClientCategory> ClientCategories
    
    // Core Methods
    static Client Create(...)              // Factory method
    void Update(...)                       // Update basic info
    void Block() / Unblock()               // Status management
    void Delete() / Restore()              // Soft delete
    void SetCreditLimit() / RemoveCreditLimit()
    void SetDelaiRetour() / ClearDelaiRetour()
    ClientCategory AddCategory()           // Assign category
    void RemoveCategory()                  // Remove assignment
    int? GetEffectiveDelaiRetour()         // Calculate effective return window
    decimal? GetEffectiveCreditLimit()     // Calculate effective credit with multiplier
    bool CanPlaceOrder()                   // Validate order eligibility
}
```

#### Category
Domain entity with business-specific rules:

```csharp
public sealed class Category
{
    // Properties
    Guid Id, string Name, string Code, int DelaiRetour,
    decimal? DiscountRate, decimal? CreditLimitMultiplier,
    bool UseBulkPricing, bool IsActive, bool IsDeleted,
    DateTime CreatedAt, DateTime? UpdatedAt
    
    // Relationships
    IReadOnlyCollection<ClientCategory> ClientCategories
    
    // Core Methods
    static Category Create(...)            // Factory method
    void Update(...)                       // Update all business properties
    void Activate() / Deactivate()         // Status management
    void Delete() / Restore()              // Soft delete
    decimal ApplyDiscount()                // Calculate discounted price
    decimal GetEffectiveCredit()           // Apply credit multiplier
    bool IsWithinDelaiRetour()             // Validate return window
}
```

#### ClientCategory
Join entity that tracks assignment metadata:

```csharp
public sealed class ClientCategory
{
    public Guid ClientId, CategoryId, AssignedById
    public DateTime AssignedAt
    public Client? Client, Category? Category
    
    internal static ClientCategory Create(...)  // Factory method
}
```

### DTOs

All DTOs are immutable records with built-in validation attributes:

- **Request DTOs**: `CreateClientRequestDto`, `UpdateClientRequestDto`, `CreateCategoryRequestDto`, etc.
- **Response DTOs**: `ClientResponseDto`, `CategoryResponseDto` with flattened data
- **Statistics DTOs**: `ClientStatsDto`, `CategoryStatsDto` with aggregated counts
- **Pagination**: `PagedResultDto<T>` with total count and page metadata

## Technologies Used

- **.NET 10.0** - Framework
- **ASP.NET Core** - Web API framework
- **Entity Framework Core 10.0** - ORM
- **SQL Server** - Database (compatible with SQL Server 2022)
- **FluentValidation** - Request validation
- **Swashbuckle/Swagger** - API documentation
- **Docker** - Containerization
- **DotNetEnv** - Environment variable management
- **Visual Studio Container Tools** - Docker tooling

## Setup Instructions

### Prerequisites
- .NET 10.0 SDK
- SQL Server 2022 (or Docker Desktop)
- Docker (optional)

### Running with Docker (Recommended)

1. Clone the repository
2. Create a `.env` file in the root directory with:
   ```
   SQLSERVER__ERPClientsDb_SA_PASSWORD=YourStrong!Passw0rd
   ```
3. Run the Docker Compose stack:
   ```bash
   docker-compose up -d
   ```
4. The service will be available at `http://localhost:5003`

### Running Locally

1. Update connection string in `appsettings.json` or environment variables
2. Apply migrations:
   ```bash
   dotnet ef database update
   ```
3. Run the application:
   ```bash
   dotnet run
   ```
4. The service will be available at `http://localhost:5157`

### Database Seeding
The application automatically seeds initial data on startup:
- **Categories**: Standard, VIP, Wholesale, Public Sector, Reseller, New Client, Legacy (inactive)
- **Clients**: 10 sample clients with various configurations (standard, VIP, wholesale, blocked, deleted, etc.)

## Example Usage

### Create a Client
```bash
POST /clients
Content-Type: application/json

{
  "name": "ABC Company",
  "email": "contact@abccompany.com",
  "address": "123 Business Avenue",
  "phone": "+216 71 123 456",
  "taxNumber": "TN12345678",
  "creditLimit": 50000,
  "delaiRetour": null
}
```

### Assign Category to Client
```bash
POST /clients/{clientId}/categories
Content-Type: application/json
X-User-Id: 00000000-0000-0000-0000-000000000001

{
  "categoryId": "category-guid-here"
}
```

### Check if Client Can Place Order
```bash
GET /clients/{clientId}/can-place-order?orderAmount=1500&currentBalance=200
```

Response:
```json
{
  "canPlace": true
}
```

### Get Client Statistics
```bash
GET /clients/stats
```

Response:
```json
{
  "totalClients": 10,
  "activeClients": 8,
  "blockedClients": 1,
  "deletedClients": 1,
  "clientsPerCategory": [
    { "categoryId": "...", "categoryName": "VIP", "clientCount": 2 },
    { "categoryId": "...", "categoryName": "Standard", "clientCount": 2 }
  ]
}
```

## Future Improvements

Based on the current code structure, potential improvements include:

1. **Event-Driven Architecture**
   - Add domain events for client/category changes (currently empty `Application/Events/` and `Infrastructure/Messaging/` folders)
   - Implement integration with message brokers for cross-service communication

2. **Caching Strategy**
   - Implement Redis caching for frequently accessed client/category data
   - Cache statistics to reduce database load

3. **API Versioning**
   - Add API versioning to support future schema changes

4. **Enhanced Validation**
   - Implement more sophisticated business rule validation
   - Add cross-field validation rules

5. **Audit Logging**
   - Add comprehensive audit trail for all state changes
   - Track historical changes to client and category assignments

6. **Performance Optimization**
   - Add indexes for frequently queried fields
   - Implement query optimization for complex joins
   - Consider read replicas for reporting queries

7. **Testing**
   - Add unit tests for domain logic
   - Add integration tests for repository and service layers
   - Implement contract tests for API endpoints

8. **Monitoring**
   - Add health checks
   - Implement distributed tracing
   - Add metrics collection for business KPIs

9. **Security**
   - Implement JWT authentication
   - Add role-based authorization
   - Encrypt sensitive data (e.g., tax numbers)