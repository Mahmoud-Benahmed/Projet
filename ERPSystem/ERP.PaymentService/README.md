# ERP.PaymentService

A .NET 8 microservice for managing payments and late fee policies, part of the ERP ecosystem.

---

## Architecture

```
ERP.PaymentService/
├── Domain/
│   ├── Entities/
│   │   ├── Payment.cs                    # Aggregate root (soft delete)
│   │   └── LateFeePolicy.cs              # Policy entity (hard delete)
│   ├── LocalCache/
│   │   ├── Invoice.cs                    # Cached Invoice projection (synced via Kafka)
│   │   └── Client.cs                     # Cached Client projection (synced via Kafka)
│   └── Enums/
│       ├── PaymentMethod.cs              # CASH, BANK_TRANSFER, CHECK
│       ├── PaymentStatus.cs              # PENDING, COMPLETED, FAILED, REFUNDED
│       └── FeeType.cs                    # PERCENTAGE, FIXED_PER_DAY
├── Application/
│   ├── DTOs/
│   │   ├── Payment/PaymentDtos.cs
│   │   └── LateFeePolicy/LateFeeePolicyDtos.cs
│   ├── Exceptions/PaymentExceptions.cs
│   ├── Interfaces/                       # All service + repository interfaces
│   └── Services/
│       ├── PaymentsService.cs            # Core payment logic + late fee + Kafka publish
│       └── LateFeeePoliciesService.cs    # Single-active-policy management
├── Controllers/
│   ├── PaymentsController.cs
│   └── LateFeePoliciesController.cs
├── Infrastructure/
│   ├── Messaging/
│   │   ├── KafkaEventPublisher.cs        # Confluent.Kafka producer
│   │   ├── InvoiceConsumer.cs            # Listens: invoice.created, invoice.updated
│   │   ├── ClientConsumer.cs             # Listens: client.updated
│   │   ├── PaymentTopics.cs              # Topic name constants
│   │   └── Events/                       # Event payload classes
│   ├── Http/
│   │   └── InvoiceServiceHttpClient.cs   # HTTP fallback for invoice fetching
│   └── Persistence/
│       ├── PaymentDbContext.cs            # EF Core DbContext + Fluent API configs
│       ├── PaymentRepository.cs
│       ├── LateFeePolicyRepository.cs
│       └── LocalCache/
│           ├── InvoiceCache/InvoiceCacheRepository.cs
│           └── ClientCache/ClientCacheRepository.cs
├── Properties/
│   └── ApiRoutes.cs                      # Route constants
├── Program.cs
├── appsettings.json
├── appsettings.Development.json
├── Dockerfile
├── docker-compose.yaml
└── ERP.PaymentService.csproj
```

---

## Kafka Flow

```
POST /api/payments
        |
        v
  PaymentsService.CreateAsync()
        |
        ├─ Fetch Invoice from local cache (InvoiceCache table)
        ├─ Validate: not CANCELLED, not already PAID
        ├─ Apply late fee if overdue (via active LateFeePolicy)
        ├─ Save Payment (Status = COMPLETED)
        ├─ Sum all completed payments for invoice
        ├─ Update InvoiceCache.TotalPaid
        |
        └─ If totalPaid >= TotalTTC:
               invoice.Status = "PAID"
               Publish → Kafka topic: "invoice.paid"
                         { InvoiceId, ClientId, TotalTTC, TotalPaid, PaidAt }

─────────────────────────────────────────────────────────

InvoiceConsumer  listens: "invoice.created", "invoice.updated"
                 → upserts Invoice into local InvoiceCache table

ClientConsumer   listens: "client.updated"
                 → upserts Client into local ClientCache table
```

---

## Endpoints

### Payments  `/api/payments`
| Method | Route | Description |
|--------|-------|-------------|
| GET    | /api/payments | Get all payments |
| GET    | /api/payments/{id} | Get by ID |
| GET    | /api/payments/invoice/{invoiceId} | Get by invoice |
| GET    | /api/payments/client/{clientId} | Get by client |
| GET    | /api/payments/status/{status} | Get by status |
| GET    | /api/payments/stats | Statistics |
| POST   | /api/payments | Create payment |
| PUT    | /api/payments/update/{id} | Update payment |
| DELETE | /api/payments/{id} | Soft delete |
| PUT    | /api/payments/{id}/restore | Restore deleted |
| GET    | /api/invoices/{id}/payment-summary | Payment summary |
| GET    | /api/invoices/{invoiceId}/payments | Payments by invoice |

### Late Fee Policies  `/api/late-fee-policies`
| Method | Route | Description |
|--------|-------|-------------|
| GET    | /api/late-fee-policies | Get all |
| GET    | /api/late-fee-policies/active | Get active policy |
| GET    | /api/late-fee-policies/{id} | Get by ID |
| POST   | /api/late-fee-policies | Create |
| PUT    | /api/late-fee-policies/update/{id} | Update |
| PUT    | /api/late-fee-policies/{id}/activate | Activate (deactivates current) |
| DELETE | /api/late-fee-policies/{id} | Hard delete |

---

## Setup

### Local Development

```bash
# 1. Set connection string in appsettings.Development.json

# 2. Run EF migrations
dotnet ef migrations add InitialCreate
dotnet ef database update

# 3. Start the service
dotnet run

# 4. Open Swagger UI
http://localhost:5038/swagger
```

### Docker

```bash
# Requires erp-network to exist
docker network create erp-network

# Create .env file
echo "SQLSERVER__SA_PASSWORD=YourStrong@Password" > .env

# Start
docker compose up -d
```

### Required Infrastructure
- **SQL Server** — via docker-compose or localhost:1433
- **Kafka** — localhost:9094 (dev) / kafka:9092 (docker)
- **InvoiceService** — running at configured `Services:InvoiceService:BaseUrl`

---

## Environment Variables (.env for Docker)

```env
SQLSERVER__SA_PASSWORD=YourStrong@Password
```
