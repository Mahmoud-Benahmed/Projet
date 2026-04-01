# ERP Auth Service - Authentication & Authorization Microservice

## 📋 Overview

The **ERP Auth Service** is a comprehensive authentication and authorization microservice built with .NET 10 and MongoDB. It provides complete user management, role-based access control (RBAC), and fine-grained privilege management for the ERP system.

### Key Features

- 🔐 **JWT Authentication** - Secure token-based authentication with refresh token rotation
- 👥 **User Management** - Create, update, delete, activate/deactivate, restore users
- 🎭 **Role Management** - Create, update, delete roles (SystemAdmin, SalesManager, StockManager, Accountant)
- 🔑 **Privilege Management** - Fine-grained permissions through control-based privileges
- 📊 **Audit Logging** - Comprehensive audit trail for all security-related actions
- 🚦 **Account Status** - Active/Inactive/Deleted states with proper business logic
- 🔄 **Token Rotation** - Secure refresh token mechanism with revocation detection

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Auth Service (Port 5188)                 │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐ │
│  │ Controllers │  │  Services   │  │   Repositories      │ │
│  │   Auth      │→│  AuthUser   │→│   MongoDB           │ │
│  │   Roles     │  │  Role       │  │   - AuthUsers       │ │
│  │   Controles │  │  Controle   │  │   - Roles           │ │
│  │   Privileges│  │  Privilege  │  │   - Controles       │ │
│  │   Audit     │  │  Audit      │  │   - Privileges      │ │
│  └─────────────┘  └─────────────┘  └─────────────────────┘ │
│         ↓                ↓                    ↓             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │              JWT Token Generator                    │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                              ↓
                    ┌─────────────────┐
                    │    MongoDB      │
                    │   ERPAuthDb     │
                    └─────────────────┘
```

## 🚀 Quick Start

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) (for containerized deployment)
- MongoDB (or Docker)

### Local Development

1. **Clone the repository**
```bash
git clone <repository-url>
cd ERP.AuthService
```

2. **Set up MongoDB**
```bash
# Using Docker
docker run -d --name mongodb -p 27017:27017 -e MONGO_INITDB_ROOT_USERNAME=root -e MONGO_INITDB_ROOT_PASSWORD=root mongo:7

# Or install MongoDB locally
```

3. **Configure environment**
Create `appsettings.Development.json` (already provided) or set environment variables:
```json
{
  "MongoSettings": {
    "ConnectionString": "mongodb://root:root@localhost:27017",
    "DatabaseName": "ERPAuthDb"
  },
  "JwtSettings": {
    "Secret": "YOUR_SUPER_SECRET_KEY_123456789",
    "Issuer": "ERP.AuthService",
    "Audience": "ERP.Client",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  }
}
```

4. **Run the service**
```bash
dotnet run
```

The service will start at `http://localhost:5188`

### Docker Deployment

```bash
# Build and run with Docker Compose
docker-compose up -d
```

The auth service will be available at `http://localhost:5001`

## 📡 API Endpoints

### Authentication

| Method | Endpoint | Description | Authorization |
|--------|----------|-------------|---------------|
| POST | `/auth/login` | User login | None |
| POST | `/auth/refresh` | Refresh access token | None |
| POST | `/auth/revoke` | Revoke refresh token | None |
| POST | `/auth/register` | Register new user | `CREATE_USER` |

### User Management

| Method | Endpoint | Description | Authorization |
|--------|----------|-------------|---------------|
| GET | `/auth` | Get all users (paginated) | `MANAGE_USERS` |
| GET | `/auth/me` | Get current user profile | Authenticated |
| GET | `/auth/{id}` | Get user by ID | `VIEW_USERS` or self |
| GET | `/auth/login/{login}` | Get user by login | `MANAGE_USERS` |
| GET | `/auth/activated` | Get active users | `MANAGE_USERS` |
| GET | `/auth/deactivated` | Get inactive users | `MANAGE_USERS` |
| GET | `/auth/deleted` | Get deleted users | `RESTORE_USER` |
| GET | `/auth/by-role` | Get users by role | `MANAGE_USERS` |
| GET | `/auth/stats` | Get user statistics | `MANAGE_USERS` |
| PUT | `/auth/update/{id}` | Update user profile | Self or `UPDATE_USER` |
| PUT | `/auth/change-password/profile` | Change own password | Authenticated |
| PUT | `/auth/change-password/{id}` | Admin change password | `UPDATE_USER` |
| PATCH | `/auth/{id}/activate` | Activate user | `ACTIVATE_USER` |
| PATCH | `/auth/{id}/deactivate` | Deactivate user | `DEACTIVATE_USER` |
| DELETE | `/auth/{id}` | Soft delete user | `DELETE_USER` |
| PATCH | `/auth/restore/{id}` | Restore deleted user | `RESTORE_USER` |

### Role Management

| Method | Endpoint | Description | Authorization |
|--------|----------|-------------|---------------|
| GET | `/auth/roles` | Get all roles | `ASSIGN_ROLES` |
| GET | `/auth/roles/paged` | Get roles (paginated) | `ASSIGN_ROLES` |
| GET | `/auth/roles/{id}` | Get role by ID | `ASSIGN_ROLES` |
| POST | `/auth/roles` | Create role | `CREATE_ROLE` |
| PUT | `/auth/roles/{id}` | Update role | `UPDATE_ROLE` |
| DELETE | `/auth/roles/{id}` | Delete role | `DELETE_ROLE` |

### Controle (Privilege) Management

| Method | Endpoint | Description | Authorization |
|--------|----------|-------------|---------------|
| GET | `/auth/controles` | Get all controles | `ASSIGN_ROLES` |
| GET | `/auth/controles/paged` | Get controles (paginated) | `ASSIGN_ROLES` |
| GET | `/auth/controles/{id}` | Get controle by ID | `ASSIGN_ROLES` |
| GET | `/auth/controles/by-category` | Get by category | `ASSIGN_ROLES` |
| POST | `/auth/controles` | Create controle | `CREATE_CONTROLE` |
| PUT | `/auth/controles/{id}` | Update controle | `UPDATE_CONTROLE` |
| DELETE | `/auth/controles/{id}` | Delete controle | `DELETE_CONTROLE` |

### Privilege Assignment

| Method | Endpoint | Description | Authorization |
|--------|----------|-------------|---------------|
| GET | `/auth/privileges/{roleId}` | Get privileges for role | `ASSIGN_ROLES` |
| PATCH | `/auth/privileges/{roleId}/{controleId}/allow` | Grant privilege | `ASSIGN_ROLES` |
| PATCH | `/auth/privileges/{roleId}/{controleId}/deny` | Deny privilege | `ASSIGN_ROLES` |

### Audit Logs

| Method | Endpoint | Description | Authorization |
|--------|----------|-------------|---------------|
| GET | `/audit` | Get all audit logs | `MANAGE_AUDITLOGS` |
| GET | `/audit/user/{userId}` | Get logs for user | `MANAGE_AUDITLOGS` |
| GET | `/audit/count` | Get log count | `MANAGE_AUDITLOGS` |
| DELETE | `/audit` | Clear all logs (dev) | `MANAGE_AUDITLOGS` |

## 🔐 Privilege System

### Privilege Categories

| Category | Privileges |
|----------|------------|
| **AUTH** | `VIEW_USERS`, `CREATE_USER`, `UPDATE_USER`, `DELETE_USER`, `ACTIVATE_USER`, `DEACTIVATE_USER`, `RESTORE_USER`, `ASSIGN_ROLES`, `CREATE_ROLE`, `UPDATE_ROLE`, `DELETE_ROLE`, `CREATE_CONTROLE`, `UPDATE_CONTROLE`, `DELETE_CONTROLE` |
| **CLIENTS** | `VIEW_CLIENTS`, `CREATE_CLIENT`, `UPDATE_CLIENT`, `DELETE_CLIENT`, `RESTORE_CLIENT`, `CREATE_CLIENT_CATEGORIES`, `UPDATE_CLIENT_CATEGORIES`, `DELETE_CLIENT_CATEGORIES`, `RESTORE_CLIENT_CATEGORIES` |
| **ARTICLES** | `VIEW_ARTICLES`, `CREATE_ARTICLE`, `UPDATE_ARTICLE`, `DELETE_ARTICLE`, `RESTORE_ARTICLE`, `CREATE_ARTICLE_CATEGORIES`, `UPDATE_ARTICLE_CATEGORIES`, `DELETE_ARTICLE_CATEGORIES`, `RESTORE_ARTICLE_CATEGORIES` |
| **FACTURATION** | `VIEW_INVOICES`, `CREATE_INVOICE`, `VALIDATE_INVOICE`, `DELETE_INVOICE`, `RESTORE_INVOICE` |
| **PAIEMENT** | `VIEW_PAYMENTS`, `RECORD_PAYMENT`, `DELETE_PAYMENT`, `RESTORE_PAYMENT` |
| **STOCK** | `VIEW_STOCK`, `UPDATE_STOCK`, `ADD_ENTRY` |
| **REPORTING** | `VIEW_REPORTS`, `EXPORT_REPORTS` |

### Built-in Roles

| Role | Privileges |
|------|------------|
| **SYSTEM_ADMIN** | All privileges |
| **SALES_MANAGER** | Full client management, article view/create/update, invoice create/view, payment view, stock view, reports view/export |
| **STOCK_MANAGER** | Full article management, full stock management, reports view |
| **ACCOUNTANT** | Audit logs, client view, invoice view/validate, full payment management, full reports |

## 🔄 Authentication Flow

### Login
```mermaid
sequenceDiagram
    Client->>AuthService: POST /auth/login
    AuthService->>MongoDB: Validate credentials
    MongoDB-->>AuthService: User found
    AuthService->>AuthService: Generate access token + refresh token
    AuthService-->>Client: { accessToken, refreshToken }
```

### Token Refresh
```mermaid
sequenceDiagram
    Client->>AuthService: POST /auth/refresh
    AuthService->>MongoDB: Validate refresh token
    MongoDB-->>AuthService: Token valid
    AuthService->>AuthService: Revoke old token, generate new pair
    AuthService-->>Client: { newAccessToken, newRefreshToken }
```

## 📊 Audit Logging

All security-sensitive operations are logged:

| Action | Description |
|--------|-------------|
| `Login` | User authentication |
| `TokenRefreshed` | Token refresh operation |
| `TokenRevoked` | Token revocation |
| `UserRegistered` | New user registration |
| `PasswordChanged` | Password change (self) |
| `PasswordChangedByAdmin` | Admin-initiated password change |
| `ProfileUpdated` | Profile information update |
| `UserActivated` | User activation |
| `UserDeactivated` | User deactivation |
| `UserDeleted` | Soft delete |
| `UserRestored` | User restoration |
| `RoleCreated` | New role creation |
| `RoleUpdated` | Role modification |
| `RoleDeleted` | Role deletion |
| `ControleCreated` | New privilege creation |
| `ControleUpdated` | Privilege modification |
| `ControleDeleted` | Privilege deletion |

## 🗄️ Database Schema

### Collections

#### AuthUsers
```json
{
  "_id": "uuid",
  "Login": "string (unique)",
  "Email": "string (unique)",
  "FullName": "string",
  "PasswordHash": "string",
  "MustChangePassword": "boolean",
  "IsActive": "boolean",
  "IsDeleted": "boolean",
  "RoleId": "uuid",
  "CreatedAt": "datetime",
  "UpdatedAt": "datetime",
  "LastLoginAt": "datetime"
}
```

#### Roles
```json
{
  "_id": "uuid",
  "Libelle": "string (unique)"
}
```

#### Controles
```json
{
  "_id": "uuid",
  "Category": "string",
  "Libelle": "string (unique)",
  "Description": "string"
}
```

#### Privileges
```json
{
  "_id": "uuid",
  "RoleId": "uuid",
  "ControleId": "uuid",
  "IsGranted": "boolean"
}
```

#### RefreshTokens
```json
{
  "_id": "uuid",
  "UserId": "uuid",
  "Token": "string (unique)",
  "ExpiresAt": "datetime",
  "IsRevoked": "boolean",
  "CreatedAt": "datetime",
  "RevokedAt": "datetime"
}
```

#### AuditLogs
```json
{
  "_id": "uuid",
  "Action": "string",
  "PerformedBy": "uuid",
  "TargetUserId": "uuid",
  "Success": "boolean",
  "FailureReason": "string",
  "IpAddress": "string",
  "UserAgent": "string",
  "Metadata": "object",
  "Timestamp": "datetime"
}
```

## 🧪 Testing

### Using HTTP Test File

The project includes `ERP.AuthService.http` with test requests:

```http
### Login
POST http://localhost:5188/auth/login
Content-Type: application/json

{
  "login": "admin_erp1234",
  "password": "Admin@1234"
}
```

### Default Test Users

After seeding, the following users are available:

| Login | Password | Role |
|-------|----------|------|
| `admin_erp1234` | `Admin@1234` | SYSTEM_ADMIN |
| `sales_erp1234` | `Sales@1234` | SALES_MANAGER |
| `stock_erp1234` | `Stock@1234` | STOCK_MANAGER |
| `account_erp1234` | `Account@1234` | ACCOUNTANT |

## 🛠️ Development

### Adding a New Privilege

1. **Add privilege constant** in `Privileges.cs`:
```csharp
public static class MyModule
{
    public const string MY_PRIVILEGE = "MY_PRIVILEGE";
}
```

2. **Add to registry** in `PrivilegeRegistry.cs`:
```csharp
new PrivilegeDefinition(Privileges.MyModule.MY_PRIVILEGE, "MY_MODULE", "Description")
```

3. **Seed with roles** in `AuthServiceSeeder.cs`:
```csharp
private static bool RoleHasPrivilege(string role, string code)
{
    return role switch
    {
        Roles.SystemAdmin => true,
        Roles.SomeRole => code == Privileges.MyModule.MY_PRIVILEGE,
        _ => false
    };
}
```

### Adding a New Role

1. **Add role constant** in `Roles.cs`:
```csharp
public const string NewRole = "NEW_ROLE";
```

2. **Define privileges** in `AuthServiceSeeder.cs`:
```csharp
private static bool RoleHasPrivilege(string role, string code)
{
    return role switch
    {
        Roles.NewRole => code switch
        {
            Privileges.SomeModule.SOME_PRIVILEGE => true,
            _ => false
        },
        // ...
    };
}
```

## 🔧 Configuration

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `MongoSettings__ConnectionString` | MongoDB connection | `mongodb://root:root@localhost:27017` |
| `MongoSettings__DatabaseName` | Database name | `ERPAuthDb` |
| `JwtSettings__Secret` | JWT signing key | Required |
| `JwtSettings__Issuer` | Token issuer | `ERP.AuthService` |
| `JwtSettings__Audience` | Token audience | `ERP.Client` |
| `JwtSettings__AccessTokenExpirationMinutes` | Access token lifetime | `15` |
| `JwtSettings__RefreshTokenExpirationDays` | Refresh token lifetime | `7` |

## 🐳 Docker Configuration

### docker-compose.yaml

```yaml
services:
  auth-service:
    build: .
    ports:
      - "5001:8080"
    environment:
      - MongoSettings__ConnectionString=mongodb://root:root@mongo:27017
      - JwtSettings__Secret=${JWT_SECRET}
    depends_on:
      - mongo
    networks:
      - erp-network

  mongo:
    image: mongo:7
    environment:
      MONGO_INITDB_ROOT_USERNAME: root
      MONGO_INITDB_ROOT_PASSWORD: root
    volumes:
      - mongo-data:/data/db
    networks:
      - erp-network
```

## 🚨 Error Responses

| Status | Code | Description |
|--------|------|-------------|
| 400 | `AUTH_013` | Invalid argument |
| 400 | `AUTH_014` | Invalid operation |
| 400 | `AUTH_016` | Validation error |
| 401 | `AUTH_002` | Invalid credentials |
| 401 | `AUTH_006` | Unauthorized access |
| 401 | `AUTH_008` | Security violation |
| 403 | `AUTH_003` | Account inactive |
| 403 | `AUTH_004` | Account already active |
| 403 | `AUTH_007` | Operation not authorized |
| 404 | `AUTH_009` | User not found |
| 404 | `AUTH_010` | Role not found |
| 404 | `AUTH_011` | Controle not found |
| 404 | `AUTH_012` | Privilege not found |
| 409 | `AUTH_001` | Email already exists |
| 409 | `AUTH_015` | Login already exists |
| 409 | `AUTH_018` | Duplicate key |

## 📦 Dependencies

- **.NET 10.0** - Runtime framework
- **MongoDB.Driver** (3.6.0) - MongoDB client
- **Microsoft.AspNetCore.Authentication.JwtBearer** (10.0.3) - JWT authentication
- **Microsoft.AspNetCore.Identity** (2.3.9) - Password hashing
- **FluentValidation** (12.1.1) - Request validation
- **Confluent.Kafka** (2.13.1) - Event publishing (optional)

## 🤝 Contributing

1. Create a feature branch
2. Make your changes
3. Update tests and documentation
4. Submit a pull request

---

## 🆘 Troubleshooting

### JWT Token Issues
- Ensure `JwtSettings:Secret` matches between gateway and auth service
- Check token expiration
- Verify issuer/audience configuration

### MongoDB Connection Issues
- Verify MongoDB is running
- Check connection string credentials
- Ensure network connectivity in Docker

### Seed Data Issues
- Check MongoDB indexes (run `MongoDbInitializer.InitializeAsync`)
- Verify no duplicate constraints violations
- Review logs for seeding errors

---

**Version**: 1.0.0  
**Target Framework**: .NET 10.0  
**Last Updated**: April 2026