---

# 🏢 ERP Microservices System

### Final Year Project – Distributed ERP Architecture

---

## 📌 Project Overview

This project is a modular **Enterprise Resource Planning (ERP)** system designed using a **Microservices Architecture** and **Event-Driven Communication**.

The system combines:

* ✅ .NET 10 Web APIs
* ✅ MongoDB & SQL Server (Database per Service)
* ✅ Apache Kafka (KRaft mode)
* ✅ Docker Compose
* ✅ API Gateway (YARP)
* ✅ JWT Authentication
* ✅ Angular Frontend (planned)

The goal is to design a **scalable, resilient, and decoupled ERP platform** aligned with modern distributed systems practices.

---

# 🏗️ Architecture Overview

## 🔹 Architectural Style

* Microservices Architecture
* Event-Driven Architecture
* Domain-Driven Design (DDD)
* Database per Service
* API Gateway Pattern

## 🔹 Communication Model

| Type  | Usage                                   |
| ----- | --------------------------------------- |
| HTTP  | Client ↔ API Gateway ↔ Services         |
| Kafka | Service ↔ Service (asynchronous events) |

---

# 📦 Services

| Service             | Database   | Responsibility                      |
| ------------------- | ---------- | ----------------------------------- |
| Auth Service        | MongoDB    | Authentication, JWT, Refresh Tokens |
| Users Service       | SQL Server | User profiles & business data       |
| Clients Service     | SQL Server | Customer management                 |
| Articles Service    | SQL Server | Product management                  |
| Facturation Service | SQL Server | Invoice management                  |
| Paiement Service    | SQL Server | Payment processing                  |
| Stock Service       | SQL Server | Inventory management                |
| Reporting Service   | SQL Server | Analytical projections              |
| API Gateway         | —          | Central entry point                 |

---

# 🔐 Auth Service

## Responsibilities

* User authentication
* JWT generation
* Refresh token management
* Role management
* Account activation / deactivation
* Password hashing (ASP.NET Identity PasswordHasher)

## Technologies

* .NET 10
* MongoDB
* JWT Bearer Authentication
* Clean Architecture layers

---

# 🧱 Project Structure (Auth Service Example)

```
ERP.AuthService
│
├── Application
│   ├── DTOs
│   ├── Interfaces
│   └── Services
│
├── Domain
│   ├── Entities
│   └── Enums
│
├── Infrastructure
│   ├── Configuration
│   ├── Persistence
│   └── Security
│
├── Controllers
├── Program.cs
└── appsettings.json
```

---

# 🔑 Authentication Flow

1. Admin registers a new user
2. Password is hashed using ASP.NET PasswordHasher
3. User logs in
4. Access Token (15 min) is generated
5. Refresh Token (7 days) is stored in MongoDB
6. Access token used to call protected endpoints

---

# 📨 Event-Driven Communication

Kafka is used to propagate domain events.

### Example Flow:

1. Facturation Service publishes:

   * `InvoiceCreated`
2. Stock Service consumes event
3. Stock quantities are updated
4. Reporting Service builds projections

---

# 🗄️ Database Strategy

The system follows:

> **Strict Database per Service Rule**

* No shared databases
* No cross-service SQL queries
* Communication only via API or Kafka events

---

# 🐳 Docker Setup

## Services in Docker Compose

* Kafka (KRaft mode)
* Kafka UI
* MongoDB
* SQL Server
* Auth Service
* Other services (planned)

To run:

```bash
docker-compose up -d
```

---
# 🚀 Running the Project

## Prerequisites

Ensure the following are installed and configured before running the project:

| Requirement | Purpose |
|---|---|
| **ASP.NET 10** | Backend runtime |
| **MongoDB, mongosh & MongoDB Compass** | Database for `AuthService` |
| **SQL Server & SSMS** | Databases for remaining services |
| **Docker Desktop** + WSL2 | Container orchestration |
| **Node.js & Angular 21** | Frontend |

> ⚠️ **Docker requirement:** Make sure **WSL2 integration** is enabled in Docker Desktop settings.

---

## Running the Project

### Option 1 — Localhost (Visual Studio)

> **Services involved:** `ERP.AuthService`, `ERP.UserService`, `ERP.Gateway`

**Step 1 — Start Kafka via Docker**

Open a WSL terminal (Ubuntu or run `bash` in cmd), navigate to the project root, and run:

```bash
user@pc:ERPSystem/$ docker compose up --build -d
```

This starts Kafka, which handles inter-service communication between `AuthService` and `UserService`. **Do not proceed until Kafka is running correctly.**

**Step 2 — Launch services in Visual Studio**

Select the `Launch Services` run profile, which starts all three services simultaneously, then hit the ▶️ **Start** button.

> 📝 **Note on data seeding:** On every startup, existing data in `ERPAuthDb` (MongoDB) and `ERPUsersDb` (SQL Server) is **wiped and re-seeded**. If you want to preserve existing data, you'll need to disable auto-seeding in `AuthService`, `UserService`, or both — otherwise the services may crash on launch if stale data is detected.

---

### Option 2 — Full Docker

**Step 1 — Open a WSL terminal** and navigate to the project root.

**Step 2 — Run the startup script:**

```bash
user@pc:ERPSystem/$ ./start-services.sh
```

**What the script does:**
- Removes any previously built service containers (e.g. `erp-auth`, `erp-users`)
- Scans for available service directories (`ERP.AuthService`, `ERP.UserService`, etc.)
- Runs `docker-compose` for each, building and starting every service container
```

Key improvements made: added a prerequisites table for scannability, used clear numbered steps with bold headers, surfaced the important data-seeding warning more prominently, clarified what the startup script does, and cleaned up typos and inconsistent formatting throughout.

## 3️⃣ Open Swagger

```
https://localhost:xxxx/swagger
```

---

# 🧪 Testing API

Using Swagger:

### Register (Admin only)

```json
{
  "email": "admin@erp.com",
  "password": "Admin123!",
  "role": 0
}
```

### Login

```json
{
  "email": "admin@erp.com",
  "password": "Admin123!"
}
```

Response:

```json
{
  "accessToken": "...",
  "refreshToken": "...",
  "expiresAt": "..."
}
```

---

# 🔒 Security Decisions

* Passwords are never stored in plain text
* Refresh tokens are stored in DB
* Access tokens are stateless JWT
* Role-based authorization
* Users can be deactivated
* MustChangePassword flag supported

---

# ⚠️ Current Implementation Status

| Feature              | Status             |
| -------------------- | ------------------ |
| Auth Service         | ✅ Implemented      |
| MongoDB Repositories | ✅ Implemented      |
| JWT Authentication   | ✅ Implemented      |
| Kafka Setup          | ⚙️ Docker prepared |
| Users Service        | 🚧 In Progress     |
| Other Services       | 🛠 Planned         |
| Angular Frontend     | 🛠 Planned         |

---

# 📈 Scalability & Future Improvements

* Kubernetes deployment
* Outbox pattern
* Dead Letter Topics
* Saga Pattern
* Schema Registry
* Centralized logging (ELK)
* Prometheus + Grafana
* Distributed tracing (OpenTelemetry)

---

# 🎓 Academic Objectives

This project demonstrates:

* Distributed systems design
* Microservices architecture
* Event-driven communication
* Secure authentication mechanisms
* Clean Architecture principles
* DevOps practices with Docker

---

# 👥 Team

* Backend Architecture & Auth: *Your Name*
* Frontend Development: *Colleague Name*

---

# 📜 License

Academic project – Educational use only.

---
