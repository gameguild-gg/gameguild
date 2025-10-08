# GameGuild Platform - AI Agent Instructions

## Architecture Overview

GameGuild is a **modular monolith** with vertical slices implemented as modules. The backend is a **.NET 9 Web API** using clean architecture patterns, and the frontend is a **Next.js 15 React app** with TypeScript.

### Key Components

- **API**: `apps/api/` - Modular .NET API with CQRS, EF Core, JWT auth, GraphQL + REST
- **Web**: `apps/web/` - Next.js app with NextAuth, Apollo GraphQL, auto-generated API clients
- **Database**: PostgreSQL with EF Core migrations and seeding

## Critical Development Workflows

### Starting the Development Environment

1. **Database**: `docker-compose up -d adminer` (starts PostgreSQL + Adminer)
2. **API**: Use VS Code task `start-api` or `dotnet run --project apps/api/GameGuild.csproj`
3. **Web**: Use VS Code task `start-web` or in `apps/web/`: `npm run dev`

The web app runs on port 3000, API on port 5000 (configurable via `.env`).

### Code Generation Workflow

The frontend uses **automatic code generation** - critical for development:

```bash
# In apps/web/
npm run api:gen        # Generates typed API client from OpenAPI spec
npm run graphql:gen    # Generates typed GraphQL hooks from schema
npm run dev            # Runs both generators + Next.js in watch mode
```

**Always run code generation after API schema changes** before writing frontend code.

### Database Migrations

```bash
# From apps/api/
dotnet ef migrations add MigrationName
dotnet ef database update
```

The API automatically applies migrations and seeds data on startup.

## Module Architecture Patterns

### Backend Module Structure (`apps/api/Source/Modules/`)

Each module follows this pattern:

```
ModuleName/
├── Commands/          # CQRS commands (mutations)
├── Queries/           # CQRS queries (reads)
├── Entities/          # Domain entities
├── Configuration/     # EF Core config & DI setup
├── Controllers/       # REST endpoints
└── GraphQL/          # GraphQL types/resolvers (optional)
```

**Key Pattern**: Modules implement `IModule` interface with `ConfigureServices()` and `MapEndpoints()` methods.

### Frontend Library Structure (`apps/web/src/lib/`)

Organized by **feature domains**, not technical layers:

```
user-management/       # Users, auth, profiles
content-management/    # Courses, projects, programs
commerce/             # Payments, subscriptions
communication/        # Posts, notifications, feeds
admin/               # Tenants, testing tools
core/                # API clients, utils, health
```

**Import Pattern**: Use feature-based imports: `from '@/lib/user-management'`

## Authentication & Authorization

### Backend: JWT + Multi-Tenant

- JWT tokens with refresh rotation
- Tenant context via `X-Tenant-Id` header
- Permission-based authorization using DAC (Discretionary Access Control)
- Multi-tenant isolation at the database level

### Frontend: NextAuth + Session Management

- NextAuth.js with JWT strategy
- Auto-refresh 30s before token expiry
- Configured API client with automatic token injection

**API Client Pattern**:

```typescript
// Use the configured authenticated client
import { configureAuthenticatedClient } from "@/lib/api/authenticated-client";

export async function myServerAction() {
  await configureAuthenticatedClient(); // Sets up auth headers
  return await someApiCall(); // Uses configured client
}
```

## Database & Entity Patterns

### Entity Framework Conventions

- **Base Entity**: All entities inherit from `EntityBase` with audit fields
- **Soft Deletes**: Global query filters automatically exclude deleted records
- **Concurrency**: Uses `Version` property for optimistic concurrency
- **Value Objects**: Email, Phone, Money as owned entities

### Migration Naming

Use descriptive names: `dotnet ef migrations add AddUserProfileCustomization`

## CQRS & Validation Patterns

### Command/Query Structure

```csharp
// Command with validator
public record CreateUserCommand(string Email, string Name) : IRequest<Result<UserDto>>;

public class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    // FluentValidation rules
}

public class CreateUserHandler : IRequestHandler<CreateUserCommand, Result<UserDto>>
{
    // Handler implementation
}
```

### Pipeline Behaviors

- **Validation**: Aggregates DataAnnotations + FluentValidation
- **Logging**: Structured logging with Serilog
- **Performance**: Measures execution time and memory

## Testing Approach

### Backend Testing Structure (`apps/api/Tests/`)

```
Core/Unit/            # Unit tests for core logic
Integration/          # Integration tests with TestHost
```

**Test Database**: Uses in-memory provider for fast tests.

### Frontend Testing

Uses Jest + Playwright for E2E testing.

## Error Handling Conventions

### Backend: Result Pattern + ProblemDetails

```csharp
public async Task<Result<UserDto>> CreateUser(CreateUserCommand command)
{
    // Returns Result<T> for success/failure handling
    // Global exception filter converts to ProblemDetails
}
```

### Frontend: Server Actions Pattern

Server actions return typed results with error handling built-in.

## Environment Configuration

### Required Environment Files

- `apps/api/.env` - Copy from `.env.example`
- Key variables: `DB_CONNECTION_STRING`, `JWT_SECRET_KEY`, OAuth secrets

### Development Defaults

- Database: `postgres/postgres/postgres` (matches docker-compose)
- API Port: 5000
- Web Port: 3000

## VS Code Integration

The workspace includes pre-configured tasks:

- `start-api` / `start-web` - Start services
- `build-api` / `build-web` - Build projects
- `ef-migration-add` / `ef-database-update` - EF Core commands

Use VS Code tasks instead of manual terminal commands when available.
