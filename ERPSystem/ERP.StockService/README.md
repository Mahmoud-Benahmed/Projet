# ERP Stock Service

A comprehensive stock management microservice for Enterprise Resource Planning (ERP), handling supplier management, stock movements (receipts, issues, returns), and integration with external services for articles and clients.

## Overview

The ERP Stock Service is a dedicated microservice responsible for managing inventory operations within an ERP ecosystem. It handles supplier (Fournisseur) management, stock receipt documents (Bon d'Entrée), stock issue documents (Bon de Sortie), and return documents (Bon de Retour). The service implements domain-driven design principles with strict business rules enforcement and maintains audit trails for all stock movements.

### Key Features
- **Supplier Management**: Create, update, block/unblock, and soft-delete suppliers with validation
- **Stock Receipts (Bon d'Entrée)**: Manage incoming stock from suppliers with line items
- **Stock Issues (Bon de Sortie)**: Manage outgoing stock to clients
- **Stock Returns (Bon de Retour)**: Handle returns from clients or to suppliers with source document validation
- **Business Rule Enforcement**: Prevent modifications to deleted documents, validate line item quantities, ensure unique document numbers
- **Return Validation**: Ensure returned quantities do not exceed source document quantities and articles exist in source documents
- **External Service Integration**: Validate articles and clients via HTTP calls to external microservices
- **Soft Delete Support**: All entities support soft deletion with restore capability
- **Pagination & Filtering**: Filter by date ranges, suppliers, clients, and deletion status

## Architecture

The project follows a **Layered Architecture** with **Domain-Driven Design (DDD)** principles, incorporating:

### Architectural Layers

| Layer | Responsibility | Key Components |
|-------|----------------|----------------|
| **Presentation** | HTTP request handling | Controllers, Routes |
| **Application** | Business logic orchestration | Services, DTOs, Interfaces |
| **Domain** | Core business rules | Entities (Fournisseur, BonEntre, BonSortie, BonRetour, Ligne*) |
| **Infrastructure** | Data persistence, external services | Repositories, DbContext, Seeders, HTTP Clients |

### Patterns Used
- **Repository Pattern**: Abstracts data access logic
- **Dependency Injection**: All dependencies are injected via constructor
- **DTO Pattern**: Separates domain models from API contracts
- **Global Exception Handling**: Centralized error handling middleware
- **Soft Delete Pattern**: Records are marked as deleted rather than removed
- **External Service Clients**: HTTP clients for article and client service integration

### Data Flow

```
HTTP Request → Controller → Service → Repository → Database
                    ↓           ↓
                 DTOs      Domain Models
                    ↓           ↓
HTTP Response ← DTOs ← Service ← Repository

External Services (Article/Client) ← Service (validation)
```

## Project Structure

```
ERP.StockService/
├── Controllers/
│   ├── FournisseurController.cs      # Supplier management
│   ├── BonEntreController.cs         # Stock receipt management
│   ├── BonSortieController.cs        # Stock issue management
│   └── BonRetourController.cs        # Stock return management
└── Properties/
│    └── ApiRoutes.cs                   # Centralized route definitions
├── Application/
│   ├── DTOs/                              # Data transfer objects
│   │   └── StockDto.cs                    # All request/response DTOs
│   ├── Exceptions/                        # Custom exceptions
│   │   ├── Exceptions.cs
│   │   ├── ArticleNotFoundException.cs
│   │   └── ClientNotFoundException.cs
│   ├── Interfaces/                        # Service and repository interfaces
│   │   ├── IStockServices.cs              # Service interfaces
│   │   └── IStockRepositories.cs          # Repository interfaces
│   └── Services/                          # Business logic implementation
│       ├── FournisseurService.cs
│       ├── BonEntreService.cs
│       ├── BonSortieService.cs
│       └── BonRetourService.cs
├── Domain/                                # Domain entities
│   ├── Fournisseur.cs                     # Supplier entity
│   ├── PieceStock.cs                      # Abstract base for stock documents
│   ├── LigneStock.cs                      # Abstract base for line items
│   ├── BonEntre.cs                        # Stock receipt document
│   ├── LigneEntre.cs                      # Receipt line item
│   ├── BonSortie.cs                       # Stock issue document
│   ├── LigneSortie.cs                     # Issue line item
│   ├── BonRetour.cs                       # Return document
│   └── LigneRetour.cs                     # Return line item
├── Infrastructure/
│   ├── Persistence/
│   │   ├── StockDbContext.cs              # EF Core DbContext
│   │   ├── Repositories/
│   │   │   ├── FournisseurRepository.cs
│   │   │   ├── BonEntreRepository.cs
│   │   │   ├── BonSortieRepository.cs
│   │   │   └── BonRetourRepository.cs
│   │   ├── Seeders/
│   │   │   └── StockDbSeeder.cs           # Database seeder
│   │   └── Messaging/                     # External service clients
│   │       ├── ArticleServiceClient.cs
│   │       └── ClientServiceHttpClient.cs
│   └── ...
├── Middleware/
│   └── GlobalExceptionMiddleware.cs       # Centralized exception handling
├── Program.cs                             # Application entry point
├── Dockerfile                             # Docker configuration
├── docker-compose.yaml                    # Multi-container orchestration
└── appsettings*.json                      # Application configuration
```

## Key Components

### Controllers

#### FournisseurController
Manages supplier operations with CRUD, blocking/unblocking, and restore capabilities.

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/stock/fournisseurs` | Get all suppliers (paginated) |
| GET | `/stock/fournisseurs/{id}` | Get supplier by ID |
| GET | `/stock/fournisseurs/deleted` | Get soft-deleted suppliers |
| GET | `/stock/fournisseurs/by-name` | Search suppliers by name |
| GET | `/stock/fournisseurs/stats` | Get supplier statistics |
| POST | `/stock/fournisseurs` | Create new supplier |
| PUT | `/stock/fournisseurs/{id}` | Update supplier |
| DELETE | `/stock/fournisseurs/{id}` | Soft delete supplier |
| PATCH | `/stock/fournisseurs/{id}/restore` | Restore supplier |
| PATCH | `/stock/fournisseurs/{id}/block` | Block supplier |
| PATCH | `/stock/fournisseurs/{id}/unblock` | Unblock supplier |

#### BonEntreController
Manages stock receipt documents.

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/stock/bon-entres` | Get all receipts (paginated) |
| GET | `/stock/bon-entres/{id}` | Get receipt by ID |
| GET | `/stock/bon-entres/deleted` | Get soft-deleted receipts |
| GET | `/stock/bon-entres/by-fournisseur/{fournisseurId}` | Get receipts by supplier |
| GET | `/stock/bon-entres/by-date-range` | Get receipts by date range |
| GET | `/stock/bon-entres/stats` | Get receipt statistics |
| POST | `/stock/bon-entres` | Create new receipt |
| PUT | `/stock/bon-entres/{id}` | Update receipt |
| DELETE | `/stock/bon-entres/{id}` | Soft delete receipt |

#### BonSortieController
Manages stock issue documents.

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/stock/bon-sorties` | Get all issues (paginated) |
| GET | `/stock/bon-sorties/{id}` | Get issue by ID |
| GET | `/stock/bon-sorties/deleted` | Get soft-deleted issues |
| GET | `/stock/bon-sorties/by-client/{clientId}` | Get issues by client |
| GET | `/stock/bon-sorties/by-date-range` | Get issues by date range |
| GET | `/stock/bon-sorties/stats` | Get issue statistics |
| POST | `/stock/bon-sorties` | Create new issue |
| PUT | `/stock/bon-sorties/{id}` | Update issue |
| DELETE | `/stock/bon-sorties/{id}` | Soft delete issue |

#### BonRetourController
Manages return documents for both client returns and supplier returns.

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/stock/bon-retours` | Get all returns (paginated) |
| GET | `/stock/bon-retours/{id}` | Get return by ID |
| GET | `/stock/bon-retours/deleted` | Get soft-deleted returns |
| GET | `/stock/bon-retours/by-source/{sourceId}` | Get returns by source document |
| GET | `/stock/bon-retours/by-date-range` | Get returns by date range |
| GET | `/stock/bon-retours/stats` | Get return statistics |
| POST | `/stock/bon-retours` | Create new return |
| PUT | `/stock/bon-retours/{id}` | Update return |
| DELETE | `/stock/bon-retours/{id}` | Soft delete return |

### Services

#### FournisseurService
- Creates/updates suppliers with validation
- Manages block/unblock status
- Handles soft delete and restore
- Provides statistics (total, active, blocked, deleted)

#### BonEntreService
- Creates receipts with line items
- Validates supplier exists and is not blocked
- Validates articles exist via external service
- Calculates total amounts
- Supports date range and supplier filtering

#### BonSortieService
- Creates issues with line items
- Validates client exists via external service
- Validates articles exist via external service
- Supports date range and client filtering

#### BonRetourService
- Creates returns from either receipts or issues
- Validates source document exists
- Validates returned articles exist in source document
- Ensures returned quantities do not exceed source quantities
- Supports source document and date range filtering

### Repositories

All repositories inherit from EF Core DbContext and provide:

- **Active Queries**: Filter out soft-deleted records by default
- **Deleted Queries**: Access soft-deleted records via `IgnoreQueryFilters()`
- **Pagination**: Consistent pagination across all entity types
- **Date Range Filtering**: Filter documents by creation date
- **Statistics**: Aggregate counts for active/deleted records

#### Special Implementation Notes

- **BonEntreRepository**: Uses `IgnoreQueryFilters()` for Fournisseur navigation to load suppliers even if they are soft-deleted, while maintaining the BonEntre-level soft delete filter
- **Split Queries**: Uses `AsSplitQuery()` to avoid cartesian explosion when loading line items

### Domain Models

#### Fournisseur (Supplier)
Central supplier entity with business rules:

```csharp
public sealed class Fournisseur
{
    Guid Id, string Name, string Address, string Phone, string? Email,
    string TaxNumber, string RIB, bool IsDeleted, bool IsBlocked,
    DateTime CreatedAt, DateTime? UpdatedAt
    
    static Fournisseur Create(...)     // Factory method
    void Update(...)                   // Update supplier info
    void Block() / Unblock()           // Status management
    void Delete() / Restore()          // Soft delete
}
```

#### PieceStock (Abstract Base)
Base class for all stock documents (BonEntre, BonSortie, BonRetour):

```csharp
public abstract class PieceStock
{
    Guid Id, string Numero, string? Observation, bool IsDeleted,
    DateTime CreatedAt, DateTime? UpdatedAt
    
    abstract void ValidateLignes()     // Validate line items
    void Delete()                      // Soft delete
    void Update(string numero, string? observation)
}
```

#### LigneStock (Abstract Base)
Base class for line items:

```csharp
public abstract class LigneStock
{
    Guid Id, Guid ArticleId, decimal Quantity, decimal Price
    
    virtual decimal CalculateTotalLigne() => Quantity * Price
    virtual void Validate()               // Validate quantity > 0, price >= 0
    virtual void Update(decimal qty, decimal price)
}
```

#### BonEntre (Stock Receipt)
Represents incoming stock from suppliers:

- **Create**: Requires unique numero, valid supplier
- **AddLigne**: Adds line items with validation
- **ValidateLignes**: Ensures at least one line item exists
- **Update**: Updates document info and clears/rebuilds line items
- **CalculateTotal**: Sum of all line item totals

#### BonSortie (Stock Issue)
Represents outgoing stock to clients:

- **Create**: Requires unique numero, valid client ID
- **AddLigne**: Adds line items with validation
- **Update**: Updates client ID, numero, and observation

#### BonRetour (Stock Return)
Handles returns from either BonEntre or BonSortie:

- **Create**: Requires unique numero, source ID, source type, and motif
- **AddLigne**: Validates article uniqueness in return document
- **Update**: Updates source document, motif, and observation
- **Source Types**: `RetourSourceType.BonSortie` (client returns) or `RetourSourceType.BonEntre` (supplier returns)

### DTOs

All DTOs are immutable records with built-in validation attributes:

- **Request DTOs**: `CreateFournisseurRequestDto`, `CreateBonEntreRequestDto`, `CreateBonSortieRequestDto`, `CreateBonRetourRequestDto`
- **Response DTOs**: `FournisseurResponseDto`, `BonEntreResponseDto`, `BonSortieResponseDto`, `BonRetourResponseDto`, `LigneResponseDto`
- **Statistics DTOs**: `FournisseurStatsDto`, `BonStatsDto`
- **Pagination**: `PagedResultDto<T>` with total count and page metadata

### External Service Clients

#### ArticleServiceClient
- Validates article existence via HTTP GET to Article Service
- Called when adding line items to any stock document
- Throws `ArticleNotFoundException` if article does not exist

#### ClientServiceHttpClient
- Validates client existence via HTTP GET to Client Service
- Called when creating or updating BonSortie documents
- Throws `ClientNotFoundException` if client does not exist

## Data Flow

### Creating a Stock Receipt (Bon d'Entrée)

1. **HTTP Request** → `BonEntreController.Create`
2. **Controller** → `BonEntreService.CreateAsync`
3. **Service** validates:
   - Supplier exists (via repository)
   - Supplier is not blocked
   - Articles exist (via external ArticleService)
4. **Service** creates domain entity and adds line items
5. **Service** validates line items (`bon.ValidateLignes()`)
6. **Repository** persists entity to database
7. **Response** returns DTO with document details

### Creating a Return (Bon de Retour)

1. **HTTP Request** → `BonRetourController.Create`
2. **Controller** → `BonRetourService.CreateAsync`
3. **Service** resolves source document (BonEntre or BonSortie)
4. **Service** validates source document exists and loads its line items
5. For each returned line item:
   - Validates article exists (via ArticleService)
   - Validates article exists in source document
   - Validates returned quantity ≤ source quantity
6. **Service** creates return document and adds validated line items
7. **Repository** persists to database
8. **Response** returns DTO with return details

## API Endpoints

### Fournisseurs (Suppliers)

| Method | Endpoint | Description | Request Body |
|--------|----------|-------------|--------------|
| POST | `/stock/fournisseurs` | Create supplier | `CreateFournisseurRequestDto` |
| GET | `/stock/fournisseurs` | Get all suppliers (paginated) | Query: page, size |
| GET | `/stock/fournisseurs/{id}` | Get supplier by ID | - |
| PUT | `/stock/fournisseurs/{id}` | Update supplier | `UpdateFournisseurRequestDto` |
| DELETE | `/stock/fournisseurs/{id}` | Soft delete supplier | - |
| PATCH | `/stock/fournisseurs/{id}/restore` | Restore supplier | - |
| PATCH | `/stock/fournisseurs/{id}/block` | Block supplier | - |
| PATCH | `/stock/fournisseurs/{id}/unblock` | Unblock supplier | - |
| GET | `/stock/fournisseurs/deleted` | Get soft-deleted suppliers | Query: page, size |
| GET | `/stock/fournisseurs/by-name` | Search suppliers by name | Query: name, page, size |
| GET | `/stock/fournisseurs/stats` | Get supplier statistics | - |

### BonEntres (Stock Receipts)

| Method | Endpoint | Description | Request Body |
|--------|----------|-------------|--------------|
| POST | `/stock/bon-entres` | Create receipt | `CreateBonEntreRequestDto` |
| GET | `/stock/bon-entres` | Get all receipts (paginated) | Query: page, size |
| GET | `/stock/bon-entres/{id}` | Get receipt by ID | - |
| PUT | `/stock/bon-entres/{id}` | Update receipt | `UpdateBonEntreRequestDto` |
| DELETE | `/stock/bon-entres/{id}` | Soft delete receipt | - |
| GET | `/stock/bon-entres/deleted` | Get soft-deleted receipts | Query: page, size |
| GET | `/stock/bon-entres/by-fournisseur/{fournisseurId}` | Get receipts by supplier | Query: page, size |
| GET | `/stock/bon-entres/by-date-range` | Get receipts by date range | Query: from, to, page, size |
| GET | `/stock/bon-entres/stats` | Get receipt statistics | - |

### BonSorties (Stock Issues)

| Method | Endpoint | Description | Request Body |
|--------|----------|-------------|--------------|
| POST | `/stock/bon-sorties` | Create issue | `CreateBonSortieRequestDto` |
| GET | `/stock/bon-sorties` | Get all issues (paginated) | Query: page, size |
| GET | `/stock/bon-sorties/{id}` | Get issue by ID | - |
| PUT | `/stock/bon-sorties/{id}` | Update issue | `UpdateBonSortieRequestDto` |
| DELETE | `/stock/bon-sorties/{id}` | Soft delete issue | - |
| GET | `/stock/bon-sorties/deleted` | Get soft-deleted issues | Query: page, size |
| GET | `/stock/bon-sorties/by-client/{clientId}` | Get issues by client | Query: page, size |
| GET | `/stock/bon-sorties/by-date-range` | Get issues by date range | Query: from, to, page, size |
| GET | `/stock/bon-sorties/stats` | Get issue statistics | - |

### BonRetours (Stock Returns)

| Method | Endpoint | Description | Request Body |
|--------|----------|-------------|--------------|
| POST | `/stock/bon-retours` | Create return | `CreateBonRetourRequestDto` |
| GET | `/stock/bon-retours` | Get all returns (paginated) | Query: page, size |
| GET | `/stock/bon-retours/{id}` | Get return by ID | - |
| PUT | `/stock/bon-retours/{id}` | Update return | `UpdateBonRetourRequestDto` |
| DELETE | `/stock/bon-retours/{id}` | Soft delete return | - |
| GET | `/stock/bon-retours/deleted` | Get soft-deleted returns | Query: page, size |
| GET | `/stock/bon-retours/by-source/{sourceId}` | Get returns by source document | Query: page, size |
| GET | `/stock/bon-retours/by-date-range` | Get returns by date range | Query: from, to, page, size |
| GET | `/stock/bon-retours/stats` | Get return statistics | - |

## Technologies Used

- **.NET 10.0** - Framework
- **ASP.NET Core** - Web API framework
- **Entity Framework Core 10.0** - ORM
- **SQL Server** - Database (compatible with SQL Server 2022)
- **Swashbuckle/Swagger** - API documentation
- **Docker** - Containerization
- **DotNetEnv** - Environment variable management
- **FluentValidation** - Request validation
- **HTTP Client** - External service integration

## Setup Instructions

### Prerequisites
- .NET 10.0 SDK
- SQL Server 2022 (or Docker Desktop)
- Article Service and Client Service running (for article/client validation)

### Configuration

The service requires configuration for:
- **Database connection**: Set in `appsettings.Development.json` or via environment variables
- **Article Service URL**: Configure `Services:ArticleService:BaseUrl`
- **Client Service URL**: Configure `Services:ClientService:BaseUrl`

### Running with Docker

1. Ensure ERP network exists:
   ```bash
   docker network create erp-network
   ```

2. Create a `.env` file in the root directory:
   ```
   SQLSERVER__ERPStockDb_SA_PASSWORD=YourStrong!Passw0rd
   ```

3. Run the Docker Compose stack:
   ```bash
   docker-compose up -d
   ```

4. The service will be available at `http://localhost:5004`

### Running Locally

1. Update connection string in `appsettings.Development.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Database=ERPStockDb;Integrated Security=True;TrustServerCertificate=True;"
     }
   }
   ```

2. Apply migrations:
   ```bash
   dotnet ef database update
   ```

3. Run the application:
   ```bash
   dotnet run --environment Development
   ```

4. The service will be available at `http://localhost:5241`

### Database Seeding
The application automatically seeds initial data on startup:
- **Fournisseurs**: Alpha, Beta, Gamma (Gamma is blocked for testing)
- **BonEntres**: 4 receipts with various line items (including one soft-deleted)
- **BonSorties**: 4 issues with various line items (including one soft-deleted)
- **BonRetours**: 3 returns (client return, supplier return, soft-deleted return)

## Example Usage

### Create a Supplier
```bash
POST /stock/fournisseurs
Content-Type: application/json

{
  "name": "Tech Supplier SARL",
  "address": "123 Industrial Zone, Tunis",
  "phone": "+216 71 123 456",
  "taxNumber": "TN98765432",
  "rib": "RIB-98765432-001",
  "email": "contact@techsupplier.tn"
}
```

### Create a Stock Receipt
```bash
POST /stock/bon-entres
Content-Type: application/json

{
  "numero": "BE-2024-001",
  "fournisseurId": "supplier-guid-here",
  "observation": "Initial stock order",
  "lignes": [
    {
      "articleId": "article-guid-1",
      "quantity": 100,
      "price": 15.50
    },
    {
      "articleId": "article-guid-2",
      "quantity": 50,
      "price": 32.00
    }
  ]
}
```

### Create a Stock Issue
```bash
POST /stock/bon-sorties
Content-Type: application/json

{
  "numero": "BS-2024-001",
  "clientId": "client-guid-here",
  "observation": "Order for client",
  "lignes": [
    {
      "articleId": "article-guid-1",
      "quantity": 10,
      "price": 25.00
    }
  ]
}
```

### Create a Return from a Stock Issue
```bash
POST /stock/bon-retours
Content-Type: application/json

{
  "numero": "BR-2024-001",
  "sourceId": "bon-sortie-guid-here",
  "sourceType": "BonSortie",
  "motif": "Damaged goods",
  "observation": "Return due to shipping damage",
  "lignes": [
    {
      "articleId": "article-guid-1",
      "quantity": 2,
      "price": 25.00,
      "remarque": "Returned 2 damaged units"
    }
  ]
}
```

### Get Supplier Statistics
```bash
GET /stock/fournisseurs/stats
```

Response:
```json
{
  "totalFournisseurs": 3,
  "activeFournisseurs": 2,
  "blockedFournisseurs": 1,
  "deletedFournisseurs": 0
}
```

## Future Improvements

Based on the current code structure, potential improvements include:

1. **Event-Driven Architecture**
   - Add domain events for stock movements to trigger inventory updates
   - Implement message broker integration (Kafka/RabbitMQ) for cross-service notifications

2. **Stock Level Management**
   - Add inventory tracking with current stock levels
   - Implement reservation system for pending orders
   - Calculate available stock based on receipts, issues, and returns

3. **Caching Strategy**
   - Cache frequently accessed suppliers and documents
   - Cache article and client validation results

4. **Enhanced Validation**
   - Implement more sophisticated business rules (e.g., return windows)
   - Add cross-document validation (e.g., cannot return more than available stock)

5. **Audit Logging**
   - Add comprehensive audit trail for all stock movements
   - Track user actions and document changes

6. **Performance Optimization**
   - Add database indexes for frequently queried fields
   - Implement query optimization for complex joins
   - Consider read replicas for reporting queries

7. **Reporting & Analytics**
   - Add inventory valuation reports
   - Implement supplier performance metrics
   - Create client purchase history analysis

8. **Testing**
   - Add unit tests for domain logic
   - Add integration tests for repository and service layers
   - Implement contract tests for API endpoints

9. **Security**
   - Implement JWT authentication
   - Add role-based authorization for document creation/approval
   - Audit user actions

10. **API Versioning**
    - Add API versioning to support future schema changes
    - Maintain backward compatibility