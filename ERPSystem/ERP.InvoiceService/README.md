# ERP Invoice Service

A comprehensive invoice management microservice for Enterprise Resource Planning (ERP), handling invoice creation, item management, invoice lifecycle (draft → unpaid → paid), and soft delete operations.

## Overview

The ERP Invoice Service is a dedicated microservice responsible for managing invoicing operations within an ERP ecosystem. It handles invoice creation with line items, invoice lifecycle management (DRAFT, UNPAID, PAID, CANCELLED), and client invoice tracking. The service implements domain-driven design principles with strict business rules enforcement.

### Key Features
- **Invoice Management**: Create, finalize, pay, and cancel invoices with full lifecycle control
- **Invoice Items**: Add and remove line items from draft invoices with automatic total calculation
- **Status Lifecycle**: Enforce strict transitions (DRAFT → UNPAID → PAID / CANCELLED)
- **Business Rule Enforcement**: Prevent modifications to non-draft invoices, validate item quantities and prices
- **Tax Calculation**: Automatic HT, TVA, and TTC computation per line item and invoice total
- **Client Tracking**: Filter invoices by client ID
- **Soft Delete Support**: Invoices support soft deletion with restore capability
- **Unique Invoice Numbers**: Prevent duplicate invoice numbers across all invoices

## Architecture

The project follows a **Layered Architecture** with **Domain-Driven Design (DDD)** principles, incorporating:

### Architectural Layers

| Layer | Responsibility | Key Components |
|-------|----------------|----------------|
| **Presentation** | HTTP request handling | Controllers, ApiRoutes |
| **Application** | Business logic orchestration | Services, DTOs, Interfaces, Exceptions |
| **Domain** | Core business rules | Invoice, InvoiceItem, InvoiceStatus, InvoiceDomainException |
| **Infrastructure** | Data persistence | Repository, DbContext, Migrations |

### Patterns Used
- **Repository Pattern**: Abstracts data access logic
- **Dependency Injection**: All dependencies injected via constructor
- **DTO Pattern**: Separates domain models from API contracts
- **Global Exception Handling**: Centralized error handling middleware
- **Soft Delete Pattern**: Records marked as deleted rather than removed

### Data Flow

```
HTTP Request → Controller → Service → Repository → Database
                    ↓           ↓
                 DTOs      Domain Models
                    ↓
HTTP Response ← DTOs ← Service ← Repository
```

## Project Structure

```
ERP.InvoiceService/
├── Controllers/
│   ├── InvoicesController.cs         # Invoice management
│   └── ApiRoutes.cs                  # Centralized route definitions
├── Application/
│   ├── DTOs/
│   │   ├── InvoiceDto.cs             # Request/response DTOs
│   │   └── InvoiceMapping.cs         # Domain → DTO mapping extensions
│   ├── Exceptions/
│   │   └── InvoiceException.cs       # Application-level exceptions
│   ├── Interfaces/
│   │   ├── IInvoiceRepository.cs     # Repository interface
│   │   └── IInvoiceService.cs        # Service interface
│   └── Services/
│       └── InvoiceService.cs         # Business logic implementation
├── Domain/
│   ├── Invoice.cs                    # Invoice aggregate root
│   ├── InvoiceItem.cs                # Invoice line item entity
│   ├── InvoiceStatus.cs              # Status enum (DRAFT, UNPAID, PAID, CANCELLED)
│   └── InvoiceDomainException.cs     # Domain rule violation exception
├── Infrastructure/
│   ├── InvoiceDbContext.cs           # EF Core DbContext
│   ├── InvoiceRepository.cs          # Repository implementation
│   └── Migrations/                   # EF Core migrations
├── Middleware/
│   └── GlobalExceptionMiddleware.cs  # Centralized exception handling
├── Program.cs                        # Application entry point
├── Dockerfile                        # Docker configuration
└── appsettings.json                  # Application configuration
```

## Key Components

### Controller

#### InvoicesController
Manages all invoice operations including lifecycle transitions and item management.

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/invoices` | Get all invoices |
| GET | `/api/invoices/{id}` | Get invoice by ID |
| GET | `/api/invoices/client/{clientId}` | Get invoices by client |
| GET | `/api/invoices/status/{status}` | Get invoices by status |
| POST | `/api/invoices` | Create new invoice |
| POST | `/api/invoices/{id}/items` | Add item to invoice |
| DELETE | `/api/invoices/{id}/items/{itemId}` | Remove item from invoice |
| PUT | `/api/invoices/{id}/finalize` | Finalize invoice (DRAFT → UNPAID) |
| PUT | `/api/invoices/{id}/pay` | Mark invoice as paid (UNPAID → PAID) |
| PUT | `/api/invoices/{id}/cancel` | Cancel invoice |
| DELETE | `/api/invoices/{id}` | Soft delete invoice |
| PUT | `/api/invoices/{id}/restore` | Restore soft-deleted invoice |

### Service

#### InvoiceService
- Creates invoices with optional initial line items
- Validates unique invoice numbers
- Manages item addition/removal (draft only)
- Enforces lifecycle transitions with domain rules
- Handles soft delete and restore

### Domain Models

#### Invoice (Aggregate Root)
Central invoice entity with strict business rules:

```csharp
public class Invoice
{
    Guid Id, string InvoiceNumber, DateTime InvoiceDate, DateTime DueDate,
    decimal TotalHT, decimal TotalTVA, decimal TotalTTC,
    InvoiceStatus Status, Guid ClientId, string ClientFullName,
    string ClientAddress, string? AdditionalNotes,
    bool IsDeleted, DateTime CreatedAt, DateTime UpdatedAt,
    IReadOnlyCollection<InvoiceItem> Items

    void AddItem(InvoiceItem item)      // Only on DRAFT
    void RemoveItem(Guid itemId)        // Only on DRAFT
    void CalculateTotals()              // Auto-calculates HT, TVA, TTC
    void FinalizeInvoice()              // DRAFT → UNPAID
    void MarkAsPaid()                   // UNPAID → PAID
    void CancelInvoice()                // DRAFT/UNPAID → CANCELLED
    void Delete() / Restore()           // Soft delete
}
```

#### InvoiceItem
Line item entity with tax calculation:

```csharp
public class InvoiceItem
{
    Guid Id, Guid InvoiceId, Guid ArticleId,
    string ArticleName, string ArticleBarCode,
    int Quantity, decimal UniPriceHT, decimal TaxRate,
    decimal TotalHT, decimal TotalTTC

    void CalculateSubtotal()   // TotalHT = Qty * UniPriceHT, TotalTTC = TotalHT * (1 + TaxRate)
}
```

#### InvoiceStatus
```csharp
public enum InvoiceStatus
{
    DRAFT,      // Initial state, items can be added/removed
    UNPAID,     // Finalized, awaiting payment
    PAID,       // Payment received
    CANCELLED   // Invoice cancelled
}
```

### Status Lifecycle

```
DRAFT ──── Finalize ──→ UNPAID ──── MarkAsPaid ──→ PAID
  │                       │
  └──── Cancel ──→ CANCELLED ←── Cancel ──┘
```

### Exceptions

| Exception | HTTP Status | Trigger |
|-----------|-------------|---------|
| `InvoiceNotFoundException` | 404 | Invoice ID not found |
| `InvoiceAlreadyExistsException` | 409 | Duplicate invoice number |
| `InvoiceInvalidOperationException` | 400 | Invalid state transition |
| `InvoiceDomainException` | 400 | Domain rule violation |

### DTOs

- **Request DTOs**: `CreateInvoiceDto`, `CreateInvoiceItemDto`, `AddInvoiceItemDto`
- **Response DTOs**: `InvoiceDto`, `InvoiceItemDto`

## API Endpoints

### Invoices

| Method | Endpoint | Description | Request Body |
|--------|----------|-------------|--------------|
| POST | `/api/invoices` | Create invoice | `CreateInvoiceDto` |
| GET | `/api/invoices` | Get all invoices | Query: `includeDeleted` |
| GET | `/api/invoices/{id}` | Get invoice by ID | - |
| GET | `/api/invoices/client/{clientId}` | Get by client | - |
| GET | `/api/invoices/status/{status}` | Get by status | - |
| POST | `/api/invoices/{id}/items` | Add item | `AddInvoiceItemDto` |
| DELETE | `/api/invoices/{id}/items/{itemId}` | Remove item | - |
| PUT | `/api/invoices/{id}/finalize` | Finalize | - |
| PUT | `/api/invoices/{id}/pay` | Mark as paid | - |
| PUT | `/api/invoices/{id}/cancel` | Cancel | - |
| DELETE | `/api/invoices/{id}` | Soft delete | - |
| PUT | `/api/invoices/{id}/restore` | Restore | - |

## Technologies Used

- **.NET 9.0** - Framework
- **ASP.NET Core** - Web API framework
- **Entity Framework Core** - ORM
- **SQL Server** - Database
- **Swashbuckle/Swagger** - API documentation
- **Docker** - Containerization
- **DotNetEnv** - Environment variable management

## Setup Instructions

### Prerequisites
- .NET 9.0 SDK
- SQL Server 2022

### Configuration

Update `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ERPInvoiceDB;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

### Running Locally

1. Apply migrations:
   ```bash
   dotnet ef database update
   ```
   Or simply run the project — migrations are applied automatically on startup.

2. Run the application:
   ```bash
   dotnet run --environment Development
   ```

3. The service will be available at `http://localhost:5037`
4. Swagger UI: `http://localhost:5037/swagger`

### Running with Docker

1. Ensure ERP network exists:
   ```bash
   docker network create erp-network
   ```

2. Run the Docker Compose stack:
   ```bash
   docker-compose up -d
   ```

## Example Usage

### Create an Invoice
```bash
POST /api/invoices
Content-Type: application/json

{
  "invoiceNumber": "INV-2024-001",
  "invoiceDate": "2024-01-15T00:00:00",
  "dueDate": "2024-02-15T00:00:00",
  "clientId": "client-guid-here",
  "clientFullName": "Ahmed Ben Ali",
  "clientAddress": "12 Rue de la République, Tunis",
  "additionalNotes": "First order",
  "items": [
    {
      "articleId": "article-guid-here",
      "articleName": "Laptop Dell XPS 15",
      "articleBarCode": "ART-001",
      "quantity": 2,
      "uniPriceHT": 400.00,
      "taxRate": 0.19
    }
  ]
}
```

### Add Item to Invoice
```bash
POST /api/invoices/{id}/items
Content-Type: application/json

{
  "articleId": "article-guid-here",
  "articleName": "Mouse Logitech MX",
  "articleBarCode": "ART-002",
  "quantity": 4,
  "uniPriceHT": 50.00,
  "taxRate": 0.19
}
```

### Finalize Invoice
```bash
PUT /api/invoices/{id}/finalize
```

### Mark Invoice as Paid
```bash
PUT /api/invoices/{id}/pay
```

## Future Improvements

1. **PDF Generation** - Export invoices as PDF documents
2. **Email Notifications** - Send invoice to client via email on finalization
3. **Payment Tracking** - Track partial payments and payment history
4. **Invoice Templates** - Support multiple invoice formats
5. **Multi-currency Support** - Handle invoices in different currencies
6. **Audit Logging** - Track all invoice state changes with user info
7. **Reporting** - Revenue reports by client, period, and status
8. **Testing** - Unit tests for domain logic and integration tests for API
9. **Event-Driven** - Publish invoice events to message broker for cross-service notifications
10. **API Versioning** - Support future schema changes with backward compatibility
