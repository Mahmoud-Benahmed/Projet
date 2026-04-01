# ERP Gateway - API Gateway for ERP Microservices

## 📋 Overview

This project is an API Gateway built with **YARP (Yet Another Reverse Proxy)** that serves as the single entry point for all ERP microservices. It handles:

- **Authentication & Authorization** via JWT tokens
- **Reverse Proxy routing** to backend services
- **Rate limiting** to prevent abuse
- **Request/Response transformation**

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────┐
│                    Clients (SPA, Mobile)            │
└─────────────────────┬───────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────┐
│                 ERP Gateway (Port 5000)             │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐   │
│  │ Auth/JWT    │ │ Rate Limit  │ │ YARP Proxy  │   │
│  └─────────────┘ └─────────────┘ └─────────────┘   │
└─────────────────────┬───────────────────────────────┘
                      │
        ┌─────────────┼─────────────┬─────────────┐
        ▼             ▼             ▼             ▼
┌──────────────┐ ┌──────────────┐ ┌──────────────┐ ┌──────────────┐
│ Auth Service │ │ Article Svc  │ │ Client Svc   │ │ Stock Svc    │
│  (Port 5188) │ │  (Port 5227) │ │  (Port 5157) │ │  (Port 5241) │
└──────────────┘ └──────────────┘ └──────────────┘ └──────────────┘
```

## 🚀 Quick Start

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Docker (for containerized deployment)

### Local Development

1. **Clone the repository**
```bash
git clone <repository-url>
cd ERP.Gateway
```

2. **Set up environment variables**
Create a `.env` file:
```env
JWT__Secret=YOUR_SUPER_SECRET_KEY_HERE
```

3. **Run the gateway**
```bash
dotnet run --launch-profile Development
```

The gateway will start at `http://localhost:5031`

### Docker Deployment

```bash
# Build and run with Docker Compose
docker-compose up -d
```

The gateway will be available at `http://localhost:5000`

## 🔧 Configuration

### Configuration Files

| File | Purpose |
|------|---------|
| `appsettings.json` | Base configuration (routes, policies) |
| `appsettings.Development.json` | Local development overrides |
| `appsettings.Docker.json` | Docker environment overrides |

### Key Configuration Sections

#### 1. **Reverse Proxy Routes**
Routes define which backend service handles each request:

```json
"Routes": {
  "articlesGetAllRoute": {
    "ClusterId": "articleCluster",
    "Match": { "Path": "/articles", "Methods": [ "GET" ] },
    "AuthorizationPolicy": "MANAGE_ARTICLES"
  }
}
```

#### 2. **Clusters**
Define backend service addresses:

```json
"Clusters": {
  "articleCluster": {
    "Destinations": {
      "articleDestination": { "Address": "http://localhost:5227" }
    }
  }
}
```

#### 3. **JWT Settings**
```json
"JWT": {
  "Secret": "YOUR_SECRET_KEY",
  "Issuer": "ERP.AuthService",
  "Audience": "ERP.Client"
}
```

## 🔐 Authorization Policies

The gateway uses a **privilege-based authorization** system:

### Privilege Categories

| Category | Privileges |
|----------|------------|
| **Users** | `VIEW_USERS`, `CREATE_USER`, `UPDATE_USER`, `DELETE_USER`, `ACTIVATE_USER`, `DEACTIVATE_USER`, `RESTORE_USER`, `ASSIGN_ROLES` |
| **Roles** | `CREATE_ROLE`, `UPDATE_ROLE`, `DELETE_ROLE` |
| **Articles** | `VIEW_ARTICLES`, `CREATE_ARTICLE`, `UPDATE_ARTICLE`, `DELETE_ARTICLE`, `RESTORE_ARTICLE` |
| **Clients** | `VIEW_CLIENTS`, `CREATE_CLIENT`, `UPDATE_CLIENT`, `DELETE_CLIENT`, `RESTORE_CLIENT` |
| **Stock** | `VIEW_STOCK`, `UPDATE_STOCK`, `ADD_ENTRY` |
| **Invoices** | `VIEW_INVOICES`, `CREATE_INVOICE`, `VALIDATE_INVOICE`, `DELETE_INVOICE` |
| **Payments** | `VIEW_PAYMENTS`, `RECORD_PAYMENT`, `DELETE_PAYMENT` |
| **Reports** | `VIEW_REPORTS`, `EXPORT_REPORTS` |
| **Audit** | `MANAGE_AUDITLOGS` |

### Composite Policies

- `MANAGE_USERS` - Any user management privilege
- `MANAGE_ARTICLES` - Any article management privilege
- `MANAGE_CLIENTS` - Any client management privilege
- `MANAGE_STOCK` - Any stock management privilege
- `MANAGE_CLIENTS_STOCK` - Clients OR stock privileges

## 🚦 Rate Limiting

| Policy | Limit | Window | Target |
|--------|-------|--------|--------|
| `LoginPolicy` | 5 attempts | 5 minutes | Login endpoint |
| `UserPolicy` | 60 requests | 1 minute (sliding) | Authenticated user endpoints |
| `WritePolicy` | 20 operations | 1 minute | POST/PUT/DELETE endpoints |
| Default | 200 requests | 1 minute | All other endpoints |

## 📡 API Endpoints

### Authentication Routes (Proxy to Auth Service)

| Method | Path | Authorization | Description |
|--------|------|---------------|-------------|
| POST | `/auth/login` | Anonymous | User login |
| POST | `/auth/register` | `CREATE_USER` | User registration |
| POST | `/auth/refresh` | Anonymous | Refresh JWT token |
| POST | `/auth/revoke` | Anonymous | Revoke refresh token |
| GET | `/auth/me` | `JwtPolicy` | Get current user profile |

### Article Management (Proxy to Article Service)

| Method | Path | Authorization | Description |
|--------|------|---------------|-------------|
| GET | `/articles` | `MANAGE_ARTICLES` | Get all articles |
| POST | `/articles` | `CREATE_ARTICLE` | Create new article |
| GET | `/articles/{id}` | `MANAGE_ARTICLES` | Get article by ID |
| PUT | `/articles/{id}` | `UPDATE_ARTICLE` | Update article |
| DELETE | `/articles/{id}` | `DELETE_ARTICLE` | Delete article |

### Client Management (Proxy to Client Service)

| Method | Path | Authorization | Description |
|--------|------|---------------|-------------|
| GET | `/clients` | `MANAGE_CLIENTS_STOCK` | Get all clients |
| POST | `/clients` | `CREATE_CLIENT` | Create new client |
| GET | `/clients/{id}` | `MANAGE_CLIENTS` | Get client by ID |
| PUT | `/clients/{id}` | `UPDATE_CLIENT` | Update client |
| DELETE | `/clients/{id}` | `DELETE_CLIENT` | Delete client |

### Stock Management (Proxy to Stock Service)

| Method | Path | Authorization | Description |
|--------|------|---------------|-------------|
| GET | `/stock/fournisseurs` | `VIEW_STOCK` | Get all suppliers |
| POST | `/stock/bon-entres` | `ADD_ENTRY` | Create stock entry |
| GET | `/stock/bon-sorties` | `VIEW_STOCK` | Get all stock outputs |

*For complete route list, see `appsettings.json`*

## 🧪 Testing

### Using the HTTP Test File

The project includes `ERP.Gateway.http` with pre-configured test requests:

```http
### Test with Admin token
GET {{gateway}}/articles
Authorization: Bearer {{adminToken}}
```

**Test tokens provided:**
- `adminToken` - Full system access
- `salesToken` - Sales manager (view-only articles)
- `stockToken` - Stock manager (view + write, no delete)
- `accountToken` - Accountant (no article access)

### Running Tests

1. Start the gateway
2. Open `ERP.Gateway.http` in Visual Studio Code or Rider
3. Click "Send Request" next to any test

## 🐳 Docker Setup

### Build Image
```bash
docker build -t erp-gateway .
```

### Environment Variables
Create `.env` file:
```
JWT__Secret=your-secret-key-min-32-characters-long
```

### Docker Compose
```yaml
services:
  gateway:
    build: .
    ports:
      - "5000:8080"
    env_file:
      - .env
    networks:
      - erp-network
```

## 🔍 Monitoring & Logging

The gateway logs:
- JWT validation failures
- Authenticated user subjects
- Rate limit rejections
- Proxy request transformations

Log output example:
```
[Gateway] JWT validation failed: IDX10223: Lifetime validation failed.
[Gateway] Token valid for sub=600d5a58-8e2f-4672-b878-df37fb6c24ae
```

## 🛠️ Development

### Adding a New Route

1. **Add route in `appsettings.json`:**
```json
"myNewRoute": {
  "ClusterId": "myCluster",
  "Match": {
    "Path": "/my-service/{**catch-all}",
    "Methods": ["GET", "POST"]
  },
  "AuthorizationPolicy": "MY_PRIVILEGE"
}
```

2. **Add privilege policy in `Program.cs`:**
```csharp
options.AddPolicy("MY_PRIVILEGE", p =>
    p.RequireAuthenticatedUser()
     .RequireClaim("privilege", "MY_PRIVILEGE"));
```

3. **Add privilege to `PrivilegeRegistry` class**

### Adding a New Backend Service

1. **Add cluster in configuration:**
```json
"Clusters": {
  "myCluster": {
    "Destinations": {
      "myDestination": { "Address": "http://my-service:8080" }
    }
  }
}
```

2. **Update environment-specific configs** (Development/Docker)

## 📦 Project Structure

```
ERP.Gateway/
├── Program.cs                 # Main application entry point
├── appsettings.json           # Base configuration
├── appsettings.Development.json # Dev overrides
├── appsettings.Docker.json    # Docker overrides
├── ERP.Gateway.csproj        # Project dependencies
├── ERP.Gateway.http          # HTTP test requests
├── Dockerfile                # Docker build instructions
├── docker-compose.yaml       # Docker Compose configuration
└── Properties/
    └── Privileges.cs         # Privilege registry
```

## 🔐 Security Considerations

1. **JWT Secret**: Use a strong, unique secret (minimum 32 characters)
2. **Environment-specific configs**: Never commit secrets to source control
3. **Rate Limiting**: Prevents brute force attacks
4. **CORS**: Restrict to trusted origins only
5. **Audit Logs**: All privileged operations are logged

## 🚨 Error Responses

| Status | Code | Description |
|--------|------|-------------|
| 401 | `AUTH_001` | Authentication required |
| 403 | `AUTH_007` | Insufficient permissions |
| 429 | `RATE_LIMIT` | Rate limit exceeded |
| 500 | - | Internal server error |

## 📚 Dependencies

- **YARP.ReverseProxy** (2.3.0) - Reverse proxy library
- **Microsoft.AspNetCore.Authentication.JwtBearer** (10.0.3) - JWT authentication
- **System.Threading.RateLimiting** (10.0.3) - Rate limiting

## 🤝 Contributing

1. Create a feature branch
2. Make your changes
3. Update tests and documentation
4. Submit a pull request

---

## 🆘 Troubleshooting

### JWT Token Issues
- Ensure `JWT:Secret` is set correctly
- Check token expiration
- Verify issuer/audience match

### Proxy Routing Issues
- Verify backend services are running
- Check cluster addresses in configuration
- Review gateway logs for routing errors

### Rate Limiting Issues
- Wait for rate limit window to reset
- Check rate limit policy applied
- Review retry-after header

---

**Version**: 1.0.0  
**Target Framework**: .NET 10.0  
**Last Updated**: April 2026