# Hizu Backend API — استودیو طراحی و ساخت دیجیتال هیزو

A modular monolith e-commerce and digital fabrication API built with **ASP.NET Core Web API (.NET 8)**, **Clean Architecture**, **CQRS with MediatR**, **Entity Framework Core**, and **PostgreSQL**.

---

## 🏗️ Architecture

The backend follows Clean Architecture with strict inward dependencies:

```text
Hizu.Domain
   ↑
Hizu.Application
   ↑
Hizu.Infrastructure

Hizu.Api
   ↓
Hizu.Application & Hizu.Infrastructure
```

- **Hizu.Domain**: Pure business entities (`Product`, `Category`, `Cart`, `Order`, `SpecialOrder`, `ApplicationUser`), value objects (`Address`), enums (`OrderStatus`, `SpecialOrderStatus`). No external dependencies.
- **Hizu.Application**: CQRS commands, queries, MediatR pipeline behaviors (`LoggingBehavior`, `ValidationBehavior`), FluentValidation rules, and abstractions (`IApplicationDbContext`).
- **Hizu.Infrastructure**: EF Core with PostgreSQL (Npgsql), ASP.NET Core Identity, JWT Bearer tokens, fluent entity configurations, database seeder, and migrations.
- **Hizu.Api**: RESTful endpoints, exception handling middleware returning RFC 7807 ProblemDetails, CORS, HealthChecks, and Swagger documentation.

---

## 🚀 Quick Start

### 1. Requirements
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- PostgreSQL running locally (Default port `5432`)

### 2. Configure Database
In `src/Hizu.Api/appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=HizuDb;Username=admin;Password=admin"
}
```

### 3. Run Migrations & Start
```bash
cd backend
dotnet ef database update --project src/Hizu.Infrastructure --startup-project src/Hizu.Api
dotnet run --project src/Hizu.Api
```

Swagger UI is available at `http://localhost:5000/swagger`.

### 4. Default Seed Accounts
- **Admin**: `admin@hizu.ir` / `Admin123!`
- **Demo Customer**: `demo@hizu.ir` / `Demo123!`

---

## 🧪 Testing
Run the automated test suite:
```bash
dotnet test
```
